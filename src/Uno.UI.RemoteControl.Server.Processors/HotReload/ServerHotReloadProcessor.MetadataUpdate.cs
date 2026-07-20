#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.HotReload;
using Uno.HotReload.Microsoft;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;
using Uno.Roslyn.MSBuild;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.HotReload;

namespace Uno.UI.RemoteControl.Host.HotReload
{
	partial class ServerHotReloadProcessor : IServerProcessor, IDisposable
	{
		private readonly BufferGate _solutionWatchersGate = new();

		private (Task<HotReloadManager> GetAsync, CancellationTokenSource Ct)? _workspace;
		private readonly IReporter _reporter = new ConsoleReporter();

		private bool _useRoslynHotReload;
		private bool _useHotReloadThruDebugger;

		private bool InitializeMetadataUpdater(ConfigureServer configureServer)
		{
			_ = bool.TryParse(_remoteControlServer.GetServerConfiguration("metadata-updates"), out _useRoslynHotReload);

			_useRoslynHotReload = _useRoslynHotReload || configureServer.EnableMetadataUpdates;
			_useHotReloadThruDebugger = configureServer.EnableHotReloadThruDebugger;

			if (_useRoslynHotReload)
			{
				InitializeInner(configureServer);

				return true;
			}
			else
			{
				return false;
			}
		}

		private void InitializeInner(ConfigureServer configureServer)
		{
			try
			{
				if (_workspace is not null)
				{
					_reporter.Warn("Hot-reload workspace is already initialized.");
					return;
				}

				// Make sure to initialize environment first.
				// This includes assembly resolution handlers required by Roslyn msbuild workspace
				CompilationEnvironment.Initialize(Path.GetDirectoryName(configureServer.ProjectPath));

				var ct = new CancellationTokenSource();
				_workspace = (InitializeAsync(ct.Token), ct);
			}
			catch (Exception e)
			{
				_reporter.Error($"Failed to initialize compilation workspace, hot-reload is disabled:\r\n{e}");
				_ = _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = false });
				_ = Notify(HotReloadEvent.Disabled);

				throw;
			}
			async Task<HotReloadManager> InitializeAsync(CancellationToken ct)
			{
				try
				{
					await Notify(HotReloadEvent.Initializing);

					var properties = configureServer.MSBuildProperties.ToDictionary();
					var runtimeTargetFramework = GetRuntimeTargetFramework(configureServer);
					var runtimeIdentifier = properties.GetValueOrDefault("RuntimeIdentifier");
					async ValueTask<Solution> LoadSolutionFromDisk(CancellationToken ct2)
					{
						var workspace = await CompilationWorkspaceProvider.CreateWorkspaceAsync(configureServer.ProjectPath, _reporter, properties, ct2);

						// Restrict a multi-targeted head to the flavor the running application reported: the
						// workspace loaded one project per TargetFrameworks entry (the evaluated TargetFramework
						// is empty), and the non-running flavors would otherwise block hot reload with their
						// compilation errors or fail the initial emit (they were never built). Then re-point the
						// kept flavor's compilation outputs to the assembly the running application was actually
						// built from (RID-specific paths) — before the watch session starts, as EnC captures its
						// baselines from those paths.
						return workspace.CurrentSolution
							.FilterHeadProjectTargetFramework(configureServer.ProjectPath, runtimeTargetFramework, _reporter)
							.AlignHeadProjectCompilationOutputs(configureServer.ProjectPath, runtimeIdentifier, _reporter, ct2);
					}

					var manager = await HotReloadManager.CreateAsync(LoadSolutionFromDisk, configureServer.MetadataUpdateCapabilities, new DelegateHotReloadHandler(SendUpdates), _tracker, ct);
					ct.Register(() => manager.Dispose());

					await _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = true });
					await Notify(HotReloadEvent.Ready);

					var fileSystemWatch = new FileSystemObserver(manager, _reporter, _solutionWatchersGate);
					ct.Register(() => fileSystemWatch.Dispose());

					return manager;
				}
				catch (Exception e)
				{
					_reporter.Error($"Failed to initialize compilation workspace, hot-reload is disabled:\r\n{e}");
					await _remoteControlServer.SendFrame(new HotReloadWorkspaceLoadResult { WorkspaceInitialized = false });
					await Notify(HotReloadEvent.Disabled);

					throw;
				}
			}
		}

		/// <summary>
		/// Resolves the target framework the connected application runs on: the value the
		/// client determined at runtime when available (see
		/// <see cref="ConfigureServer.RuntimeTargetFramework"/>), otherwise — for older
		/// clients — the <c>TargetFramework</c> MSBuild property captured at build time.
		/// </summary>
		private static string? GetRuntimeTargetFramework(ConfigureServer configureServer)
		{
			if (configureServer.RuntimeTargetFramework is { Length: > 0 } runtimeTargetFramework)
			{
				return runtimeTargetFramework;
			}

			return configureServer.MSBuildProperties.TryGetValue("TargetFramework", out var captured) && captured is { Length: > 0 }
				? captured
				: null;
		}

		private async ValueTask SendUpdates(ImmutableHashSet<string> files, ImmutableArray<Update> updates, CancellationToken ct)
		{
#if DEBUG
			_reporter.Output($"Sending {updates.Length} metadata updates for {string.Join(",", files.Select(Path.GetFileName))}");
#endif

			for (var i = 0; i < updates.Length; i++)
			{
				var update = updates[i];
				var moduleId = update.ModuleId.ToString();
				var metadataDelta = Convert.ToBase64String(update.MetadataDelta.AsSpan());
				var ilDelta = Convert.ToBase64String(update.ILDelta.AsSpan());
				var pdbDelta = Convert.ToBase64String(update.PdbDelta.AsSpan());
				var updatedTypes = Convert.ToBase64String(GetLengthPrefixedArray(update.UpdatedTypes));

				// if the app is running from VSCode with the mono debugger attached
				if (_useHotReloadThruDebugger)
				{
					// send metadataDelta, ilDelta and pdbDelta thru the IDE channel to be applied by the debugger
					if (!await _remoteControlServer.TrySendMessageToIDEAsync(
						new Uno.UI.RemoteControl.Messaging.IdeChannel.HotReloadThruDebuggerIdeMessage(
							moduleId,
							metadataDelta,
							ilDelta,
							pdbDelta
						),
						ct))
					{
						throw new InvalidOperationException("No active connection with the IDE to send update thru debugger.");
					}
					// send the updatedTypes thru the regular hot reload channel to notify the app about the changes
					await _remoteControlServer.SendFrame(
						new AssemblyDeltaReload
						{
							FilePaths = files,
							ModuleId = moduleId,
							UpdatedTypes = updatedTypes,
						});
				}
				else
				{
					await _remoteControlServer.SendFrame(
						new AssemblyDeltaReload
						{
							FilePaths = files,
							ModuleId = moduleId,
							PdbDelta = pdbDelta,
							ILDelta = ilDelta,
							MetadataDelta = metadataDelta,
							UpdatedTypes = updatedTypes,
						});
				}
			}

			static byte[] GetLengthPrefixedArray(ImmutableArray<int> values)
			{
				var result = new byte[sizeof(int) /* length */ + values.Length * sizeof(int)];
				BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(0), values.Length);
				for (var i = 0; i < values.Length; i++)
				{
					BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan((i + 1) * sizeof(int)), values[i]);
				}
				return result;
			}
		}
	}
}
