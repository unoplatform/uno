using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Tasks.Deployment.ManifestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.Extensions.Logging;
using Uno.Disposables;
using Uno.Extensions;
using Uno.HotReload;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.HotReload;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.Tasks.HotReloadInfo;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Uno.HotReload.Utils.RoslynExtensions;

[assembly: Uno.UI.RemoteControl.Host.ServerProcessor<Uno.UI.RemoteControl.Host.HotReload.ServerHotReloadProcessor>]

namespace Uno.UI.RemoteControl.Host.HotReload
{
	partial class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private static readonly TimeSpan _waitForIdeResultTimeout = TimeSpan.FromSeconds(25);
		private readonly CancellationTokenSource _ct = new();
		private readonly IRemoteControlServer _remoteControlServer;
		private readonly ITelemetry _telemetry;
		private readonly HotReloadTracker _tracker;

		public ServerHotReloadProcessor(IRemoteControlServer remoteControlServer, ITelemetry<ServerHotReloadProcessor> telemetry)
		{
			_remoteControlServer = remoteControlServer;
			_telemetry = telemetry;
			_tracker = new(async (status, ct) => await _remoteControlServer.SendFrame(new HotReloadStatusMessage(status)), ct => RequestHotReloadToIde(), _reporter);
		}

		public string Scope => WellKnownScopes.HotReload;

		// Properties configured on processor initialization (by the ConfigureServer message)
		// WARNING: Those properties might change!
		private ConfigureServer? _config;
		private bool _isRunningInsideVisualStudio;

		private void InitializeProcessor(ConfigureServer config)
		{
			// This is called from ProcessConfigureServer() during initialization.
			// WARNING: It's possible that this method is called multiple times! (HD will invoke this among the standard init)

			_config = config;

			_isRunningInsideVisualStudio = config.MSBuildProperties.TryGetValue("BuildingInsideVisualStudio", out var isVsRaw)
				&& isVsRaw.Equals("true", StringComparison.OrdinalIgnoreCase);
		}

		public async Task ProcessFrame(Frame frame)
		{
			// Messages received from the CLIENT application
			switch (frame.Name)
			{
				case ConfigureServer.Name:
					await ProcessConfigureServer(frame.GetContent<ConfigureServer>());
					break;
				case XamlLoadError.Name:
					ProcessXamlLoadError(frame.GetContent<XamlLoadError>());
					break;
				case UpdateSingleFileRequest.Name:
					await ProcessUpdateFile(frame.GetContent<UpdateSingleFileRequest>());
					break;
				case UpdateFileRequest.Name:
					await ProcessUpdateFile(frame.GetContent<UpdateFileRequest>());
					break;
				//case PackWorkspaceRequest.Name:
				//	await ProcessPackWorkspaceAsync(frame.GetContent<PackWorkspaceRequest>(), _ct.Token);
				//	break;
				//case LoadWorkspaceRequest.Name:
				//	await ProcessLoadWorkspaceAsync(frame.GetContent<LoadWorkspaceRequest>(), _ct.Token);
				//	break;
			}
		}

		/// <inheritdoc />
		public async Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
		{
			// Messages received from the IDE
			switch (message)
			{
				case IdeResultMessage resultMessage:
					if (_pendingRequestsToIde.TryGetValue(resultMessage.IdeCorrelationId, out var tcs))
					{
						tcs.TrySetResult(resultMessage.Result);
					}
					break;

				case HotReloadEventIdeMessage evt:
					await Notify(evt.Event, HotReloadEventSource.IDE);
					break;
			}
		}

		#region Hot-reload state
		public enum HotReloadEventSource
		{
			IDE,
			DevServer
		}

		private async ValueTask Notify(HotReloadEvent evt, HotReloadEventSource source = HotReloadEventSource.DevServer)
		{
			var properties = new Dictionary<string, string>
			{
				["Event"] = evt.ToString(),
				["Source"] = source.ToString(),
				["PreviousState"] = _tracker.State.ToString()
			};


			Dictionary<string, double>? measurements = null;
			if (_tracker.Current != null)
			{
				measurements = new Dictionary<string, double>
				{
					["FileCount"] = _tracker.Current.ConsideredFilePaths.Count
				};
				if (_tracker.Current.CompletionTime != null)
				{
					var duration = (_tracker.Current.CompletionTime.Value - _tracker.Current.StartTime).TotalMilliseconds;
					measurements["DurationMs"] = duration;
				}
			}

			_telemetry.TrackEvent("notify-start", properties, measurements);

			try
			{
				switch (evt)
				{
					// Global state events
					case HotReloadEvent.Disabled:
						await _tracker.SetStateAsync(Uno.HotReload.Tracking.HotReloadState.Disabled);
						_telemetry.TrackEvent("notify-disabled", properties, measurements);
						break;

					case HotReloadEvent.Initializing:
						await _tracker.SetStateAsync(Uno.HotReload.Tracking.HotReloadState.Initializing);
						_telemetry.TrackEvent("notify-initializing", properties, measurements);
						break;

					case HotReloadEvent.Ready:
						await _tracker.SetStateAsync(Uno.HotReload.Tracking.HotReloadState.Ready);
						_telemetry.TrackEvent("notify-ready", properties, measurements);
						break;

					// Pending hot-reload events
					case HotReloadEvent.ProcessingFiles:
						await _tracker.EnsureHotReloadStarted();
						_telemetry.TrackEvent("notify-processing-files", properties, measurements);
						break;

					case HotReloadEvent.Completed:
						await (await _tracker.StartOrContinueHotReload()).DeferComplete(HotReloadOperationResult.Success);
						_telemetry.TrackEvent("notify-completed", properties, measurements);
						break;

					case HotReloadEvent.NoChanges:
						await (await _tracker.StartOrContinueHotReload()).Complete(HotReloadOperationResult.NoChanges);
						_telemetry.TrackEvent("notify-no-changes", properties, measurements);
						break;

					case HotReloadEvent.Failed:
						await (await _tracker.StartOrContinueHotReload()).Complete(HotReloadOperationResult.Failed);
						_telemetry.TrackEvent("notify-failed", properties, measurements);
						break;

					case HotReloadEvent.RudeEdit:
						await (await _tracker.StartOrContinueHotReload()).Complete(HotReloadOperationResult.RudeEdit);
						_telemetry.TrackEvent("notify-rude-edit", properties, measurements);
						break;
				}

				properties["NewState"] = _tracker.State.ToString();
				properties["HasCurrentOperation"] = (_tracker.Current != null).ToString();
				_telemetry.TrackEvent("notify-complete", properties, measurements);
			}
			catch (Exception ex)
			{
				var errorProperties = new Dictionary<string, string>(properties)
				{
					["ErrorMessage"] = ex.Message,
					["ErrorType"] = ex.GetType().Name
				};
				_telemetry.TrackEvent("notify-error", errorProperties, measurements);
				throw;
			}
		}
		#endregion

		#region XamlLoadError
		private void ProcessXamlLoadError(XamlLoadError xamlLoadError)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError(
					$"The XAML file failed to load [{xamlLoadError.FilePath}]\n" +
					$"{xamlLoadError.ExceptionType}: {xamlLoadError.Message}\n" +
					$"{xamlLoadError.StackTrace}");
			}
		}
		#endregion

		#region ConfigureServer
		private async Task ProcessConfigureServer(ConfigureServer configureServer)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Base project path: {configureServer.ProjectPath}");
			}

			InitializeProcessor(configureServer);

			try
			{
				if (InitializeMetadataUpdater(configureServer))
				{
					this.Log().LogDebug($"Metadata updater initialized");
				}
				else
				{
					// We are relying on IDE, we won't have any other hot-reload initialization steps.
					await Notify(HotReloadEvent.Ready);
					this.Log().LogDebug("Metadata updater **NOT** initialized.");
				}
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError(error, "Failed to configure metadata updater");
				}

				await SafeNotifyMetadataInitializationFailed(error);
				throw;
			}
		}

		private async Task SafeNotifyMetadataInitializationFailed(Exception? ex = null)
		{
			var errorMessage = ex?.Message ?? ex?.ToString();

			try
			{
				await _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = false });
			}
			catch (Exception sendError)
			{
				this.Log().LogWarning(sendError, "Failed to send workspace failure notification to client");
			}

			try
			{
				// Send the error via HotReloadStatusMessage immediately
				await _tracker.SendUpdate(serverError: errorMessage);

				// Then notify the disabled state
				await Notify(HotReloadEvent.Disabled);
			}
			catch (Exception notifyError)
			{
				this.Log().LogWarning(notifyError, "Failed to notify hot-reload disabled state");
			}
		}
		#endregion

		#region UpdateFile
		// LEGACY: As the Update file message might have been duplicated in other projects (e.g. runtime tests engine), we make sure to stay backward compatible.
		private async Task ProcessUpdateFile(UpdateSingleFileRequest singleRequest)
		{
			var multiRequest = new UpdateFileRequest
			{
				RequestId = singleRequest.RequestId,
				ForceSaveOnDisk = singleRequest.ForceSaveOnDisk,
				IsForceHotReloadDisabled = singleRequest.IsForceHotReloadDisabled,
				ForceHotReloadDelay = singleRequest.ForceHotReloadDelay,
				ForceHotReloadAttempts = singleRequest.ForceHotReloadAttempts,
				Edits = [new(singleRequest.FilePath, singleRequest.OldText, singleRequest.NewText, singleRequest.IsCreateDeleteAllowed)],
			};
			var multiResponse = await DoProcessUpdateFileRequest(multiRequest);
			var singleResult = multiResponse.Results.SingleOrDefault();
			var singleResponse = new UpdateSingleFileResponse(
				singleRequest.RequestId,
				singleRequest.FilePath,
				singleResult?.Result ?? FileUpdateResult.Failed,
				multiResponse.GlobalError ?? singleResult?.Error,
				multiResponse.HotReloadCorrelationId);

			await _remoteControlServer.SendFrame(singleResponse);
		}

		private async Task ProcessUpdateFile(UpdateFileRequest request)
			=> await _remoteControlServer.SendFrame(await DoProcessUpdateFileRequest(request));

		private async Task<UpdateFileResponse> DoProcessUpdateFileRequest(UpdateFileRequest request)
		{
			if (request.Edits.IsDefaultOrEmpty)
			{
				return new UpdateFileResponse(request.RequestId, "No edit to process", []);
			}
			else if (request.Edits.DistinctBy(edit => Path.GetFullPath(edit.FilePath), PathComparer.Comparer).Count() != request.Edits.Length)
			{
				return new UpdateFileResponse(request.RequestId, "Detected multiple updates on the same file", []);
			}

			var hotReload = await _tracker.StartHotReload([.. request.Edits.Select(edit => Path.GetFullPath(edit.FilePath))]);
			var results = ImmutableArray<FileEditResult>.Empty;
			try
			{
				using var _ = _solutionWatchersGate.Acquire(); // Makes sure to batch all file changes in a single solution update

				results = [.. await Task.WhenAll(request.Edits.Select(edit => DoEditFile(edit, request.ForceSaveOnDisk, request.RequestId)))];

				// Update the hot-reload info to the application will be able to determine the request has been applied
				await WriteHotReloadInfo(request, hotReload);

				// Forcefully request a hot-reload after the file edits have been applied (only if at least one edit succeed).
				if (request.IsForceHotReloadDisabled is false && results.Any(result => (int)result.Result < 300))
				{
					if ((request.ForceHotReloadDelay ?? HotReloadOperation.DefaultAutoRetryIfNoChangesDelay) is { TotalMilliseconds: > 0 } delay)
					{
						await Task.Delay(delay);
					}

					hotReload.EnableAutoRetryIfNoChanges(request.ForceHotReloadAttempts, request.ForceHotReloadDelay);

					// Even if IDE does not support hot-reload manual requests, we still invoke this to report the HR processingFiles state as soon as possible.
					await RequestHotReloadToIde();
				}

				return new UpdateFileResponse(request.RequestId, null, results, hotReload.Id);
			}
			catch (Exception ex)
			{
				await hotReload.Complete(HotReloadOperationResult.InternalError, ex);
				return new UpdateFileResponse(request.RequestId, ex.Message, results, hotReload.Id);
			}
		}

		private async Task<FileEditResult> DoEditFile(FileEdit edit, bool? forceSaveOnDisk, string reqIdForLogging)
		{
			try
			{
				var (result, error) = edit switch
				{
					{ FilePath: null or { Length: 0 } } => (FileUpdateResult.BadRequest, "Invalid request (file path is empty)"),
					{ OldText: not null, NewText: not null } when _isRunningInsideVisualStudio => await DoRemoteUpdateInIde(edit.NewText),
					{ OldText: not null, NewText: not null } => await DoUpdateOnDisk(edit.OldText, edit.NewText),
					{ OldText: null, NewText: not null } when _isRunningInsideVisualStudio => await DoRemoteUpdateInIde(edit.NewText),
					{ OldText: null, NewText: not null } => await DoWriteToDisk(edit.NewText),
					{ NewText: null, IsCreateDeleteAllowed: true } => await DoDeleteFromDisk(),
					_ => (FileUpdateResult.BadRequest, "Invalid request")
				};
				return new(edit.FilePath!, result, error);
			}
			catch (Exception ex)
			{
				return new(edit.FilePath, FileUpdateResult.Failed, ex.Message);
			}

			async ValueTask<(FileUpdateResult, string?)> DoRemoteUpdateInIde(string newText)
			{
				var saveToDisk = forceSaveOnDisk ?? true; // Temporary set to true until this issue is fixed: https://github.com/unoplatform/uno.hotdesign/issues/3454

				// Right now, when running on VS, we're delegating the file update to the code that is running inside VS.
				// we're not doing this for other file operations because they are not/less required for hot-reload. We may need to revisit this eventually.
				var ideMsg = new UpdateFileIdeMessage(GetNextIdeCorrelationId(), edit.FilePath, newText, saveToDisk);
				var result = await SendAndWaitForResult(ideMsg);

				return result.IsSuccess
					? (FileUpdateResult.Success, null)
					: (FileUpdateResult.Failed, result.Error);
			}

			async Task<(FileUpdateResult, string?)> DoUpdateOnDisk(string oldText, string newText)
			{
				if (!File.Exists(edit.FilePath))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"[{reqIdForLogging}] Requested file '{edit.FilePath}' does not exists.");
					}

					return (FileUpdateResult.FileNotFound, $"Requested file '{edit.FilePath}' does not exists.");
				}

				var originalContent = await File.ReadAllTextAsync(edit.FilePath);
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().LogTrace($"[{reqIdForLogging}] Original content of '{edit.FilePath}': {originalContent}.");
				}

				var updatedContent = originalContent.Replace(oldText, newText);
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().LogTrace($"[{reqIdForLogging}] Updated content of '{edit.FilePath}': {updatedContent}.");
				}

				if (updatedContent == originalContent)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"[{reqIdForLogging}] No changes detected in {edit.FilePath}.");
					}

					return (FileUpdateResult.NoChanges, null);
				}

				var effectiveUpdate = WaitForFileUpdated(edit.FilePath, reqIdForLogging);
				await File.WriteAllTextAsync(edit.FilePath, updatedContent);
				await effectiveUpdate;

				return (FileUpdateResult.Success, null);
			}

			async ValueTask<(FileUpdateResult, string?)> DoWriteToDisk(string newText)
			{
				if (!edit.IsCreateDeleteAllowed && !File.Exists(edit.FilePath))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"[{reqIdForLogging}] Requested file '{edit.FilePath}' does not exists.");
					}

					return (FileUpdateResult.FileNotFound, $"Requested file '{edit.FilePath}' does not exists.");
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().LogTrace($"[{reqIdForLogging}] Write content of '{edit.FilePath}': {newText}.");
				}

				var effectiveUpdate = WaitForFileUpdated(edit.FilePath, reqIdForLogging);
				await File.WriteAllTextAsync(edit.FilePath, newText);
				await effectiveUpdate;

				return (FileUpdateResult.Success, null);
			}

			async ValueTask<(FileUpdateResult, string?)> DoDeleteFromDisk()
			{
				if (!File.Exists(edit.FilePath))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"[{reqIdForLogging}] Requested file '{edit.FilePath}' does not exists.");
					}

					return (FileUpdateResult.FileNotFound, $"Requested file '{edit.FilePath}' does not exists.");
				}

				var effectiveUpdate = WaitForFileUpdated(edit.FilePath, reqIdForLogging);
				File.Delete(edit.FilePath);
				await effectiveUpdate;

				return (FileUpdateResult.Success, null);
			}
		}

		private async ValueTask WaitForFileUpdated(string filePath, string? editTagForLogging)
		{
			var file = new FileInfo(filePath);
			var dir = file.Directory;
			while (dir is { Exists: false })
			{
				dir = dir.Parent;
			}

			if (dir is null)
			{
				return;
			}

			var tcs = new TaskCompletionSource();
			using var watcher = new FileSystemWatcher(dir.FullName);
			watcher.Changed += (snd, e) =>
			{
				if (e.FullPath.Equals(file.FullName, StringComparison.OrdinalIgnoreCase))
				{
					tcs.TrySetResult();
				}
			};
			watcher.EnableRaisingEvents = true;

			if (await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5))) != tcs.Task
				&& this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"File update event not received for '{filePath}', continuing anyway [{editTagForLogging}].");
			}
		}

		private async ValueTask WriteHotReloadInfo(IUpdateFileRequest request, HotReloadOperation hotReload)
		{
			if (_config?.HotReloadInfoPath is not { Length: > 0 } path)
			{
				return;
			}

			var effectiveUpdate = WaitForFileUpdated(path, request.RequestId);
			await File.WriteAllTextAsync(path, HotReloadInfoHelper.GenerateInfo(hotReload.Id, request.RequestId));
			await effectiveUpdate;
		}

		private async ValueTask<bool> RequestHotReloadToIde()
		{
			var result = await SendAndWaitForResult(new ForceHotReloadIdeMessage(GetNextIdeCorrelationId()));

			if (result.IsSuccess)
			{
				// Note: For now the IDE will notify the ProcessingFiles only in case of force hot reload request sent by client!
				await Notify(HotReloadEvent.ProcessingFiles, HotReloadEventSource.IDE);
			}

			return result.IsSuccess;
		}
		#endregion

		//private async Task ProcessPackWorkspaceAsync(PackWorkspaceRequest req, CancellationToken ct)
		//{
		//	try
		//	{
		//		if (await GetWorkspaceAsync() is { } workspace)
		//		{
		//			var packagePath = await WorkspacePackage.Create(workspace.CurrentSolution, req.TargetFile, true, ct);
		//			await _remoteControlServer.SendFrame(new PackWorkspaceResponse(req.RequestId, packagePath, null));
		//		}
		//		else
		//		{
		//			await _remoteControlServer.SendFrame(new PackWorkspaceResponse(req.RequestId, null, Error: "Hot-reload workspace not initialized"));
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		await _remoteControlServer.SendFrame(new PackWorkspaceResponse(req.RequestId, null, Error: $"Pack failed\r\n{ex}"));
		//	}
		//}

		//private async Task ProcessLoadWorkspaceAsync(LoadWorkspaceRequest req, CancellationToken ct)
		//{
		//	try
		//	{
		//		if (_configureServer is null)
		//		{
		//			throw new InvalidOperationException("Server not configured yet");
		//		}

		//		// Abort current workspace
		//		await (_workspace?.Ct.CancelAsync() ?? Task.CompletedTask);

		//		var manifest = await WorkspacePackage.Extract(req.PackageFile, req.WorkingDir, true, ct);
		//		var workspaceCt = new CancellationTokenSource();
		//		var manager = await CreateAdHoc(_configureServer, manifest, ct);
		//		workspaceCt.Token.Register(() => manager.Dispose());

		//		var fileSystemWatch = new FileSystemObserver(manager, _reporter, _solutionWatchersGate);
		//		ct.Register(() => fileSystemWatch.Dispose());

		//		_originalWorkspace ??= await GetWorkspaceAsync();
		//		_workspace = new(Task.FromResult(manager), workspaceCt);

		//		await _remoteControlServer.SendFrame(new LoadWorkspaceResponse(req.RequestId, Error: null));
		//	}
		//	catch (Exception ex)
		//	{
		//		await _remoteControlServer.SendFrame(new LoadWorkspaceResponse(req.RequestId, Error: $"Load failed\r\n{ex}"));
		//	}
		//}

		public void Dispose()
		{
			_ct.Cancel();
			_workspace?.Ct.Cancel();
		}

		#region Helpers - IDE Channel SendAndWaitForResult
		private readonly ConcurrentDictionary<long, TaskCompletionSource<Result>> _pendingRequestsToIde = new();

		private long _lasIdeCorrelationId;

		private long GetNextIdeCorrelationId() => Interlocked.Increment(ref _lasIdeCorrelationId);

		private async Task<Result> SendAndWaitForResult<TMessage>(TMessage message)
			where TMessage : IdeMessageWithCorrelationId
		{
			var tcs = new TaskCompletionSource<Result>();
			try
			{
				using var cts = new CancellationTokenSource(_waitForIdeResultTimeout);
				await using var ctReg = cts.Token.Register(() => tcs.TrySetCanceled());

				_pendingRequestsToIde.TryAdd(message.CorrelationId, tcs);
				if (await _remoteControlServer.TrySendMessageToIDEAsync(message, cts.Token))
				{
					return await tcs.Task;
				}
				else
				{
					return new Result("No IDE connection to send the message.");
				}
			}
			catch (Exception ex)
			{
				return Result.Fail(ex);
			}
			finally
			{
				_pendingRequestsToIde.TryRemove(message.CorrelationId, out _);
			}
		}
		#endregion
	}
}
