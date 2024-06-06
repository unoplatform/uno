using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elfie.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.HotReload;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

[assembly: Uno.UI.RemoteControl.Host.ServerProcessorAttribute(typeof(Uno.UI.RemoteControl.Host.HotReload.ServerHotReloadProcessor))]

namespace Uno.UI.RemoteControl.Host.HotReload
{
	partial class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private FileSystemWatcher[]? _watchers;
		private CompositeDisposable? _watcherEventsDisposable;
		private IRemoteControlServer _remoteControlServer;

		public ServerHotReloadProcessor(IRemoteControlServer remoteControlServer)
		{
			_remoteControlServer = remoteControlServer;
		}

		public string Scope => WellKnownScopes.HotReload;

		public async Task ProcessFrame(Frame frame)
		{
			switch (frame.Name)
			{
				case ConfigureServer.Name:
					ProcessConfigureServer(frame.GetContent<ConfigureServer>());
					break;
				case XamlLoadError.Name:
					ProcessXamlLoadError(frame.GetContent<XamlLoadError>());
					break;
				case UpdateFile.Name:
					await ProcessUpdateFile(frame.GetContent<UpdateFile>());
					break;
			}
		}

		/// <inheritdoc />
		public async Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
		{
			switch (message)
			{
				case HotReloadRequestedIdeMessage hrRequested:
					// Note: For now the IDE will notify the ProcessingFiles only in case of force hot reload request sent by client!
					await Notify(HotReloadEvent.ProcessingFiles, HotReloadEventSource.IDE);
					if (_pendingHotReloadRequestToIde.TryGetValue(hrRequested.RequestId, out var request))
					{
						request.TrySetResult(hrRequested.Result);
					}
					break;

				case HotReloadEventIdeMessage evt:
					await Notify(evt.Event, HotReloadEventSource.IDE);
					break;
			}
		}

		#region Hot-relaod state
		private HotReloadState _globalState; // This actually contains only the initializing stat (i.e. Disabled, Initializing, Idle). Processing state is _current != null.
		private HotReloadServerOperation? _current; // I.e. head of the operation chain list

		public enum HotReloadEventSource
		{
			IDE,
			DevServer
		}

		private async ValueTask EnsureHotReloadStarted()
		{
			if (_current is null)
			{
				await StartHotReload(null);
			}
		}

		private async ValueTask<HotReloadServerOperation> StartHotReload(ImmutableHashSet<string>? filesPaths)
		{
			var previous = _current;
			HotReloadServerOperation? current, @new;
			while (true)
			{
				@new = new HotReloadServerOperation(this, previous, filesPaths);
				current = Interlocked.CompareExchange(ref _current, @new, previous);
				if (current == previous)
				{
					break;
				}
				else
				{
					previous = current;
				}
			}

			// Notify the start of new hot-reload operation
			await SendUpdate();

			return @new;
		}

		private async ValueTask<HotReloadServerOperation> StartOrContinueHotReload(ImmutableHashSet<string>? filesPaths = null)
			=> _current is { } current && (filesPaths is null || current.TryMerge(filesPaths))
				? current
				: await StartHotReload(filesPaths);

		private ValueTask AbortHotReload()
			=> _current?.Complete(HotReloadServerResult.Aborted) ?? SendUpdate();

		private async ValueTask Notify(HotReloadEvent evt, HotReloadEventSource source = HotReloadEventSource.DevServer)
		{
			switch (evt)
			{
				// Global state events
				case HotReloadEvent.Disabled:
					_globalState = HotReloadState.Disabled;
					await AbortHotReload();
					break;

				case HotReloadEvent.Initializing:
					_globalState = HotReloadState.Initializing;
					await SendUpdate();
					break;

				case HotReloadEvent.Ready:
					_globalState = HotReloadState.Idle;
					await SendUpdate();
					break;

				// Pending hot-reload events
				case HotReloadEvent.ProcessingFiles:
					await EnsureHotReloadStarted();
					break;

				case HotReloadEvent.Completed:
					await (await StartOrContinueHotReload()).DeferComplete(HotReloadServerResult.Success);
					break;

				case HotReloadEvent.NoChanges:
					await (await StartOrContinueHotReload()).Complete(HotReloadServerResult.NoChanges);
					break;
				case HotReloadEvent.Failed:
					await (await StartOrContinueHotReload()).Complete(HotReloadServerResult.Failed);
					break;

				case HotReloadEvent.RudeEdit:
				case HotReloadEvent.RudeEditDialogButton:
					await (await StartOrContinueHotReload()).Complete(HotReloadServerResult.RudeEdit);
					break;
			}
		}

		private async ValueTask SendUpdate(HotReloadServerOperation? completing = null)
		{
			var state = _globalState;
			var operations = ImmutableList<HotReloadServerOperationData>.Empty;

			if (state is not HotReloadState.Disabled && (_current ?? completing) is { } current)
			{
				var infos = ImmutableList.CreateBuilder<HotReloadServerOperationData>();
				var foundCompleting = completing is null;
				LoadInfos(current);
				if (!foundCompleting)
				{
					LoadInfos(completing);
				}

				operations = infos.ToImmutable();

				void LoadInfos(HotReloadServerOperation? operation)
				{
					while (operation is not null)
					{
						if (operation.Result is null)
						{
							state = HotReloadState.Processing;
						}

						foundCompleting |= operation == completing;
						infos.Add(new(operation.Id, operation.StartTime, operation.FilePaths, operation.CompletionTime, operation.Result));
						operation = operation.Previous!;
					}
				}
			}

			await _remoteControlServer.SendFrame(new HotReloadStatusMessage(state, operations));
		}

		/// <summary>
		/// A hot-reload operation that is in progress.
		/// </summary>
		private class HotReloadServerOperation
		{
			// Delay to wait without any update to consider operation was aborted.
			private static readonly TimeSpan _timeoutDelay = TimeSpan.FromSeconds(30);

			private static readonly ImmutableHashSet<string> _empty = ImmutableHashSet<string>.Empty.WithComparer(_pathsComparer);
			private static long _count;

			private readonly ServerHotReloadProcessor _owner;
			private readonly HotReloadServerOperation? _previous;
			private readonly Timer _timeout;

			private ImmutableHashSet<string> _filePaths;
			private int /* HotReloadResult */ _result = -1;
			private CancellationTokenSource? _deferredCompletion;

			public long Id { get; } = Interlocked.Increment(ref _count);

			public DateTimeOffset StartTime { get; } = DateTimeOffset.Now;

			public DateTimeOffset? CompletionTime { get; private set; }

			public HotReloadServerOperation? Previous => _previous;

			public ImmutableHashSet<string> FilePaths => _filePaths;

			public HotReloadServerResult? Result => _result is -1 ? null : (HotReloadServerResult)_result;

			/// <param name="previous">The previous hot-reload operation which has to be considered as aborted when this new one completes.</param>
			public HotReloadServerOperation(ServerHotReloadProcessor owner, HotReloadServerOperation? previous, ImmutableHashSet<string>? filePaths = null)
			{
				_owner = owner;
				_previous = previous;
				_filePaths = filePaths ?? _empty;

				_timeout = new Timer(
					static that => _ = ((HotReloadServerOperation)that!).Complete(HotReloadServerResult.Aborted),
					this,
					_timeoutDelay,
					Timeout.InfiniteTimeSpan);
			}

			/// <summary>
			/// Attempts to update the <see cref="FilePaths"/> if we determine that the provided paths are corresponding to this operation.
			/// </summary>
			/// <returns>
			/// True if this operation should be considered as valid for the given file paths (and has been merged with original paths),
			/// false if the given paths does not belong to this operation.
			/// </returns>
			public bool TryMerge(ImmutableHashSet<string> filePaths)
			{
				if (_result is not -1)
				{
					return false;
				}

				var original = _filePaths;
				while (true)
				{
					ImmutableHashSet<string> updated;
					if (original.IsEmpty)
					{
						updated = filePaths;
					}
					else if (original.Any(filePaths.Contains))
					{
						updated = original.Union(filePaths);
					}
					else
					{
						return false;
					}

					var current = Interlocked.CompareExchange(ref _filePaths, updated, original);
					if (current == original)
					{
						_timeout.Change(_timeoutDelay, Timeout.InfiniteTimeSpan);
						return true;
					}
					else
					{
						original = current;
					}
				}
			}

			// Note: This is a patch until the dev-server based hot-reload treat files per batch instead of file per file.
			private HotReloadServerResult _aggregatedResult = HotReloadServerResult.NoChanges;
			private int _aggregatedFilesCount;
			public void NotifyIntermediate(string file, HotReloadServerResult result)
			{
				if (Interlocked.Increment(ref _aggregatedFilesCount) is 1)
				{
					_aggregatedResult = result;
					return;
				}

				_aggregatedResult = (HotReloadServerResult)Math.Max((int)_aggregatedResult, (int)result);
				_timeout.Change(_timeoutDelay, Timeout.InfiniteTimeSpan);
			}

			public async ValueTask CompleteUsingIntermediates()
			{
				Debug.Assert(_aggregatedFilesCount == _filePaths.Count);
				await Complete(_aggregatedResult);
			}

			/// <summary>
			/// As errors might get a bit after the complete from the IDE, we can defer the completion of the operation.
			/// </summary>
			public async ValueTask DeferComplete(HotReloadServerResult result, Exception? exception = null)
			{
				Debug.Assert(result != HotReloadServerResult.InternalError || exception is not null); // For internal error we should always provide an exception!

				if (Interlocked.CompareExchange(ref _deferredCompletion, new CancellationTokenSource(), null) is null)
				{
					_timeout.Change(_timeoutDelay, Timeout.InfiniteTimeSpan);
					await Task.Delay(TimeSpan.FromSeconds(1), _deferredCompletion.Token);
					if (!_deferredCompletion.IsCancellationRequested)
					{
						await Complete(result, exception);
					}
				}
			}

			public ValueTask Complete(HotReloadServerResult result, Exception? exception = null)
				=> Complete(result, exception, isFromNext: false);

			private async ValueTask Complete(HotReloadServerResult result, Exception? exception, bool isFromNext)
			{
				Debug.Assert(result != HotReloadServerResult.InternalError || exception is not null); // For internal error we should always provide an exception!

				// Remove this from current
				Interlocked.CompareExchange(ref _owner._current, null, this);
				_deferredCompletion?.Cancel(false); // No matter if already completed

				// Check if not already disposed
				if (Interlocked.CompareExchange(ref _result, (int)result, -1) is not -1)
				{
					return; // Already completed
				}

				CompletionTime = DateTimeOffset.Now;
				await _timeout.DisposeAsync();

				// Consider previous hot-reload operation(s) as aborted (this is actually a chain list)
				if (_previous is not null)
				{
					await _previous.Complete(
						HotReloadServerResult.Aborted,
						new TimeoutException("An more recent hot-reload operation has completed."),
						isFromNext: true);
				}

				if (!isFromNext) // Only the head of the list should request update
				{
					await _owner.SendUpdate(this);
				}
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
		private void ProcessConfigureServer(ConfigureServer configureServer)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Base project path: {configureServer.ProjectPath}");
				this.Log().LogDebug($"Xaml Search Paths: {string.Join(", ", configureServer.XamlPaths)}");
			}

			if (!InitializeMetadataUpdater(configureServer))
			{
				// We are relying on IDE (or XAML only), we won't have any other hot-reload initialization steps.
				_ = Notify(HotReloadEvent.Ready);
			}

			_watchers = configureServer.XamlPaths
				.Select(p => new FileSystemWatcher
				{
					Path = p,
					Filter = "*.*",
					NotifyFilter = NotifyFilters.LastWrite |
						NotifyFilters.Attributes |
						NotifyFilters.Size |
						NotifyFilters.CreationTime |
						NotifyFilters.FileName,
					EnableRaisingEvents = true,
					IncludeSubdirectories = false
				})
				.ToArray();

			_watcherEventsDisposable = new CompositeDisposable();

			foreach (var watcher in _watchers)
			{
				var disposable = ToObservable(watcher).Subscribe(
					filePaths =>
					{
						var files = filePaths
							.Distinct()
							.Where(f =>
								Path.GetExtension(f).Equals(".xaml", StringComparison.OrdinalIgnoreCase)
								|| Path.GetExtension(f).Equals(".cs", StringComparison.OrdinalIgnoreCase));

						foreach (var file in files)
						{
							OnSourceFileChanged(file);
						}
					},
					e => Console.WriteLine($"Error {e}"));

				_watcherEventsDisposable.Add(disposable);
			}

			void OnSourceFileChanged(string fullPath)
				=> Task.Run(async () =>
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"File {fullPath} changed");
					}

					await _remoteControlServer.SendFrame(
						new FileReload
						{
							Content = File.ReadAllText(fullPath),
							FilePath = fullPath
						});
				});
		}
		#endregion

		#region UpdateFile
		private readonly ConcurrentDictionary<long, TaskCompletionSource<Result>> _pendingHotReloadRequestToIde = new();

		private async Task ProcessUpdateFile(UpdateFile message)
		{
			var hotReload = await StartHotReload(ImmutableHashSet<string>.Empty.Add(Path.GetFullPath(message.FilePath)));

			try
			{
				var (result, error) = DoUpdateFile();
				if ((int)result < 300 && !message.IsForceHotReloadDisabled)
				{
					await RequestHotReloadToIde(hotReload.Id);
				}

				await _remoteControlServer.SendFrame(new UpdateFileResponse(message.RequestId, message.FilePath, result, error, hotReload.Id));
			}
			catch (Exception ex)
			{
				await hotReload.Complete(HotReloadServerResult.InternalError, ex);
				await _remoteControlServer.SendFrame(new UpdateFileResponse(message.RequestId, message.FilePath, FileUpdateResult.Failed, ex.Message));
			}

			(FileUpdateResult, string?) DoUpdateFile()
			{
				if (message?.IsValid() is not true)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Got an invalid update file frame ({message})");
					}

					return (FileUpdateResult.BadRequest, "Invalid request");
				}

				if (!File.Exists(message.FilePath))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Requested file '{message.FilePath}' does not exists.");
					}

					return (FileUpdateResult.FileNotFound, $"Requested file '{message.FilePath}' does not exists.");
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Apply Changes to {message.FilePath}");
				}

				var originalContent = File.ReadAllText(message.FilePath);
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().LogTrace($"Original content: {message.FilePath}");
				}

				var updatedContent = originalContent.Replace(message.OldText, message.NewText);
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().LogTrace($"Updated content: {message.FilePath}");
				}

				if (updatedContent == originalContent)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"No changes detected in {message.FilePath}");
					}

					return (FileUpdateResult.NoChanges, null);
				}

				File.WriteAllText(message.FilePath, updatedContent);
				return (FileUpdateResult.Success, null);
			}
		}

		private async Task<bool> RequestHotReloadToIde(long sequenceId)
		{
			var hrRequest = new ForceHotReloadIdeMessage(sequenceId);
			var hrRequested = new TaskCompletionSource<Result>();

			try
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
				await using var ctReg = cts.Token.Register(() => hrRequested.TrySetCanceled());

				await _remoteControlServer.SendMessageToIDEAsync(hrRequest);

				return await hrRequested.Task is { IsSuccess: true };
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				_pendingHotReloadRequestToIde.TryRemove(hrRequest.CorrelationId, out _);
			}
		}
		#endregion

		public void Dispose()
		{
			_watcherEventsDisposable?.Dispose();

			if (_watchers != null)
			{
				foreach (var watcher in _watchers)
				{
					watcher.Dispose();
				}
			}

			_solutionWatcherEventsDisposable?.Dispose();
			if (_solutionWatchers != null)
			{
				foreach (var watcher in _solutionWatchers)
				{
					watcher.Dispose();
				}
			}

			_hotReloadService?.EndSession();
		}

		#region Helpers
		private static IObservable<IList<string>> ToObservable(FileSystemWatcher watcher)
			=> Observable.Create<string>(o =>
			{
				// Create an observable instead of using the FromEventPattern which
				// does not register to events properly.
				// Renames are required for the WriteTemporary->DeleteOriginal->RenameToOriginal that
				// Visual Studio uses to save files.

				void changed(object s, FileSystemEventArgs args) => o.OnNext(args.FullPath);
				void renamed(object s, RenamedEventArgs args) => o.OnNext(args.FullPath);

				watcher.Changed += changed;
				watcher.Created += changed;
				watcher.Renamed += renamed;

				return Disposable.Create(() =>
				{
					watcher.Changed -= changed;
					watcher.Created -= changed;
					watcher.Renamed -= renamed;
				});
			}).Buffer(TimeSpan.FromMilliseconds(250));
		#endregion
	}
}
