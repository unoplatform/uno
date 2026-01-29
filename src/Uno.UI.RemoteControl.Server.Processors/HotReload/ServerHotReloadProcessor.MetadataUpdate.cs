#nullable enable

using System;
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

					var properties = GetWorkspaceProperties(configureServer, out var outputPaths);
					async ValueTask<Workspace> CreateMsBuildWorkspace(CancellationToken ct2)
						=> await CompilationWorkspaceProvider.CreateWorkspaceAsync(configureServer.ProjectPath, _reporter, properties, ct2);

					var manager = await HotReloadManager.CreateAsync(CreateMsBuildWorkspace, configureServer.MetadataUpdateCapabilities, SendUpdates, _tracker, ct);
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

		private static Dictionary<string, string> GetWorkspaceProperties(ConfigureServer configureServer, out string?[] outputPaths)
		{
			// Clone the properties from the ConfigureServer
			var properties = configureServer.MSBuildProperties.ToDictionary();

			// Flag the current build as created for hot reload, which allows for running targets or settings
			// props/items in the context of the hot reload workspace.
			properties["UnoIsHotReloadHost"] = "True";

			// If the runtime identifier NOT been used in the output path, this usually indicates that it was not passed as a parameter for the build
			// in that case we **must** not use it to init the hot-reload workspace (parameters are required to be exactly the same to get valid patches)
			// Note: This is required to get HR to work on Rider 2024.3 with Android
			// Note 2: We remove both properties to make sure to use the default behavior
			var appendIdToPath = properties.Remove("AppendRuntimeIdentifierToOutputPath", out var appendStr)
				&& bool.TryParse(appendStr, out var append)
				&& append;
			var hasOutputPath = properties.Remove("OutputPath", out var outputPath);
			properties.Remove("IntermediateOutputPath", out var intermediateOutputPath);

			if (properties.Remove("RuntimeIdentifier", out var runtimeIdentifier))
			{
				if (appendIdToPath && hasOutputPath && Path.TrimEndingDirectorySeparator(outputPath ?? "").EndsWith(runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
				{
					// Set the RuntimeIdentifier as a temporary property so that we do not force the
					// property as a read-only global property that would be transitively applied to
					// projects that are not supporting the head's RuntimeIdentifier. (e.g. an android app
					// which references a netstd2.0 library project)
					properties["UnoHotReloadRuntimeIdentifier"] = runtimeIdentifier;
				}
			}

			// Pass the TargetFramework as a temporary property so that we do not force the tfm for all projects, but only the head project
			// (that references the Dev Server assembly which includes the target file to promote back the UnoHotReloadTargetFramework as TargetFramework).
			// This is required to make sure that an application referencing a class-lib project targeting a different TFM (e.g. net10 while head is net10-desktop)
			// can still be hot-reloaded.
			if (properties.Remove("TargetFramework", out var targetFramework))
			{
				properties["UnoHotReloadTargetFramework"] = targetFramework;
				properties["TargetFrameworks"] = targetFramework;
			}

			outputPaths = [Trim(outputPath), Trim(intermediateOutputPath)];
			return properties;

			// We make sure to trim the output path from any TFM / RID / Configuration suffixes
			// This is to make sure that if we have multiple active HR workspace (like an old Android emulator reconnecting while a desktop app is running),
			// we will not consider the files of the other targets.
			string? Trim(string? outDir)
			{
				var result = outDir;
				while (!string.IsNullOrWhiteSpace(result))
				{
					var updated = result
						.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
						.TrimEnd(targetFramework, PathComparer.Comparison)
						.TrimEnd(runtimeIdentifier, PathComparer.Comparison)
						.TrimEnd(properties.GetValueOrDefault("Configuration"), PathComparer.Comparison);
					if (updated == result)
					{
						return result + Path.DirectorySeparatorChar; // We make sure to restore the dir separator at the end to make sure filters applies only on folders!
					}
					else
					{
						result = updated;
					}
				}

				return null;
			}
		}

		private async ValueTask SendUpdates(ImmutableHashSet<string> files, ImmutableArray<Update> updates, CancellationToken ct)
		{
#if DEBUG
			_reporter.Output($"Sending {updates.Length} metadata updates for {string.Join(",", files.Select(Path.GetFileName))}");
#endif

			for (var i = 0; i < updates.Length; i++)
			{
				if (_useHotReloadThruDebugger)
				{
					if (!await _remoteControlServer.TrySendMessageToIDEAsync(
							new Uno.UI.RemoteControl.Messaging.IdeChannel.HotReloadThruDebuggerIdeMessage(
								updates[i].ModuleId.ToString(),
								Convert.ToBase64String(updates[i].MetadataDelta.ToArray()),
								Convert.ToBase64String(updates[i].ILDelta.ToArray()),
								Convert.ToBase64String(updates[i].PdbDelta.ToArray())
							),
							ct))
					{
						throw new InvalidOperationException("No active connection with the IDE to send update thru debugger.");
					}
				}
				else
				{
					var updateTypesWriterStream = new MemoryStream();
					var updateTypesWriter = new BinaryWriter(updateTypesWriterStream);
					WriteIntArray(updateTypesWriter, updates[i].UpdatedTypes.ToArray());

					await _remoteControlServer.SendFrame(
						new AssemblyDeltaReload
						{
							FilePaths = files,
							ModuleId = updates[i].ModuleId.ToString(),
							PdbDelta = Convert.ToBase64String(updates[i].PdbDelta.ToArray()),
							ILDelta = Convert.ToBase64String(updates[i].ILDelta.ToArray()),
							MetadataDelta = Convert.ToBase64String(updates[i].MetadataDelta.ToArray()),
							UpdatedTypes = Convert.ToBase64String(updateTypesWriterStream.ToArray()),
						});
				}
			}
		}

		static void WriteIntArray(BinaryWriter binaryWriter, int[] values)
		{
			if (values is null)
			{
				binaryWriter.Write(0);
				return;
			}

			binaryWriter.Write(values.Length);
			foreach (var value in values)
			{
				binaryWriter.Write(value);
			}
		}
	}
}
