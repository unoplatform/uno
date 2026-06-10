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
using Uno.HotReload.Info;
using Uno.HotReload.IO;
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
		private FileUpdater _fileUpdater;

		public ServerHotReloadProcessor(IRemoteControlServer remoteControlServer, ITelemetry<ServerHotReloadProcessor> telemetry)
		{
			_remoteControlServer = remoteControlServer;
			_telemetry = telemetry;
			_tracker = new(async (status, ct) => await _remoteControlServer.SendFrame(new HotReloadStatusMessage(status)), ct => RequestHotReloadToIde(), _reporter);
			_fileUpdater = CreateFileUpdater(isRunningInsideVisualStudio: false, hotReloadInfoPath: null);
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

			_fileUpdater = CreateFileUpdater(_isRunningInsideVisualStudio, config.HotReloadInfoPath);
		}

		private FileUpdater CreateFileUpdater(bool isRunningInsideVisualStudio, string? hotReloadInfoPath)
		{
			var onDisk = new OnDiskFileEditor(_reporter);
			IFileEditor editor = isRunningInsideVisualStudio
				? new IDEFileEditor(
					async (filePath, newText, saveToDisk) =>
					{
						var result = await SendAndWaitForResult(new UpdateFileIdeMessage(GetNextIdeCorrelationId(), filePath, newText, saveToDisk));
						return (result.IsSuccess, result.Error);
					},
					onDisk)
				: onDisk;

			return new FileUpdater(
				editor,
				_solutionWatchersGate,
				_tracker,
				new HotReloadInfoFile(hotReloadInfoPath, _reporter),
				async () => { await RequestHotReloadToIde(); });
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
			var response = await _fileUpdater.UpdateAsync(singleRequest, _ct.Token);
			var singleResult = response.Results.SingleOrDefault();
			var singleResponse = new UpdateSingleFileResponse(
				singleRequest.RequestId,
				singleRequest.FilePath,
				singleResult?.Result ?? FileUpdateResult.Failed,
				response.GlobalError ?? singleResult?.Error,
				response.HotReloadCorrelationId);

			await _remoteControlServer.SendFrame(singleResponse);
		}

		private async Task ProcessUpdateFile(UpdateFileRequest request)
		{
			var response = await _fileUpdater.UpdateAsync(request, _ct.Token);
			await _remoteControlServer.SendFrame(new UpdateFileResponse(
				response.RequestId,
				response.GlobalError,
				response.Results,
				response.HotReloadCorrelationId));
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
