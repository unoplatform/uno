
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor
	{
		private bool _linkerEnabled;
		private HotReloadAgent? _agent;
		private MetadataUpdatesSupport _supportsMetadataUpdates; // Indicates that we **expect** to get metadata updates for HR for the current environment (dev-server or VS)
		private bool _serverMetadataUpdatesEnabled; // Indicates that the dev-server has been configured to generate metadata updates on file changes
		private bool _runningInsideVSCodeExtension; // running with Uno VS Code extension allows Hot Reload to work while debugging
		private readonly TaskCompletionSource<bool> _hotReloadWorkloadSpaceLoaded = new();

		private void WorkspaceLoadResult(HotReloadWorkspaceLoadResult hotReloadWorkspaceLoadResult)
		{
			// If we get a workspace loaded message, we can assume that we are running with the dev-server
			// This mean that HR won't work with the debugger attached.
			if (Debugger.IsAttached && !_runningInsideVSCodeExtension)
			{
				_status.ReportInvalidRuntime();
			}
			_hotReloadWorkloadSpaceLoaded.SetResult(hotReloadWorkspaceLoadResult.WorkspaceInitialized);
		}

		/// <summary>
		/// Waits for the server's hot reload workspace to be loaded
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public Task<bool> WaitForWorkspaceLoaded(CancellationToken ct)
			=> _hotReloadWorkloadSpaceLoaded.Task;

		[MemberNotNull(nameof(_agent))]
		partial void InitializeMetadataUpdater()
		{
			_instance = this;

			CheckMetadataUpdatesSupport();

			_linkerEnabled = string.Equals(Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_LINKER_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);

			if (_linkerEnabled)
			{
				var message = "The application was compiled with the IL linker enabled, hot reload is disabled. " +
							"See WasmShellILLinkerEnabled for more details.";

				Console.WriteLine($"[ERROR] {message}");
			}

			_agent = new HotReloadAgent(s =>
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace(s);
				}
			});
		}

		private void CheckMetadataUpdatesSupport()
		{
			// Single layer uno runtime identifier
			var unoRuntimeIdentifier = GetMSBuildProperty("UnoRuntimeIdentifier");

			// two layer uno runtime identifier, UnoWinRTRuntimeIdentifier defines the actual running target.
			var unoWinRTRuntimeIdentifier = GetMSBuildProperty("UnoWinRTRuntimeIdentifier");

			var buildingInsideVisualStudio = GetMSBuildProperty("BuildingInsideVisualStudio").Equals("true", StringComparison.OrdinalIgnoreCase);
			// This is only set when Uno's mono debugger is used inside VS Code
			_runningInsideVSCodeExtension = Environment.GetEnvironmentVariable("__UNO_SUPPORT_DEBUG_HOT_RELOAD__") == "true";

			var unoEffectiveRuntimeIdentifier = string.IsNullOrWhiteSpace(unoWinRTRuntimeIdentifier)
				? unoRuntimeIdentifier
				: unoWinRTRuntimeIdentifier;

			var isForcedMetadata = _forcedHotReloadMode is HotReloadMode.MetadataUpdates;
			var isSkia = unoEffectiveRuntimeIdentifier.Equals("skia", StringComparison.OrdinalIgnoreCase);
			var isWasm = unoEffectiveRuntimeIdentifier.Equals("webassembly", StringComparison.OrdinalIgnoreCase);

			var devServerEnabled = isForcedMetadata

				// CoreCLR Debugger under VS Win already handles metadata updates
				// CoreCLR Debugger under VS Code prevents metadata based hot reload
				|| (!Debugger.IsAttached && !buildingInsideVisualStudio && isSkia)

				// Uno's Mono Debugger under VS Code handles metadata based hot reload
				|| _runningInsideVSCodeExtension

				// Mono Debugger under VS Win already handles metadata updates
				// Mono Debugger under Rider prevents metadata based hot reload
				|| (!Debugger.IsAttached && !buildingInsideVisualStudio && isWasm)
				|| (!Debugger.IsAttached && !buildingInsideVisualStudio && OperatingSystem.IsAndroid())
				|| (!Debugger.IsAttached && !buildingInsideVisualStudio && OperatingSystem.IsIOS());

			var vsEnabled = isForcedMetadata
				|| (buildingInsideVisualStudio && isSkia)
				|| (buildingInsideVisualStudio && isWasm)
				|| (buildingInsideVisualStudio && Debugger.IsAttached && OperatingSystem.IsAndroid())
				|| (buildingInsideVisualStudio && Debugger.IsAttached && OperatingSystem.IsIOS());

			_supportsMetadataUpdates = devServerEnabled || vsEnabled;
			_serverMetadataUpdatesEnabled = devServerEnabled;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ServerMetadataUpdates Enabled:{_serverMetadataUpdatesEnabled} DebuggerAttached:{Debugger.IsAttached} BuildingInsideVS: {buildingInsideVisualStudio} RunningInsideVSCodeExtension: {_runningInsideVSCodeExtension} unorid: {unoRuntimeIdentifier}");
			}
		}

		private string[] GetMetadataUpdateCapabilities()
		{
			if (Type.GetType(HotReloadAgent.MetadataUpdaterType) is { } type)
			{
				if (type.GetMethod("GetCapabilities", BindingFlags.Static | BindingFlags.NonPublic) is { } getCapabilities)
				{
					if (getCapabilities.Invoke(null, Array.Empty<string>()) is string caps)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Metadata Updates runtime capabilities: {caps}");
						}

						return caps.Split(' ');
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().Trace($"Runtime does not support Hot Reload (Invalid returned type for {HotReloadAgent.MetadataUpdaterType}.GetCapabilities())");
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Trace($"Runtime does not support Hot Reload (Unable to find method {HotReloadAgent.MetadataUpdaterType}.GetCapabilities())");
					}
				}
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Trace($"Runtime does not support Hot Reload (Unable to find type {HotReloadAgent.MetadataUpdaterType})");
				}
			}
			return Array.Empty<string>();
		}

		private void ProcessAssemblyReload(AssemblyDeltaReload assemblyDeltaReload)
		{
			try
			{
				if (Debugger.IsAttached)
				{
					// the work is done elsewhere but we don't want to report an error
					if (!_runningInsideVSCodeExtension)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error("Hot Reload is not supported when the debugger is attached.");
						}
						_status.ReportLocalStarting([]).ReportIgnored("Hot Reload is not supported when the debugger is attached");
					}

					return;
				}

				if (assemblyDeltaReload.IsValid())
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Applying IL Delta after {string.Join(",", assemblyDeltaReload.FilePaths)}, Guid:{assemblyDeltaReload.ModuleId}");
					}

					var changedTypesStreams = new MemoryStream(Convert.FromBase64String(assemblyDeltaReload.UpdatedTypes));
					var changedTypesReader = new BinaryReader(changedTypesStreams);

					var delta = new UpdateDelta
					{
						MetadataDelta = Convert.FromBase64String(assemblyDeltaReload.MetadataDelta),
						ILDelta = Convert.FromBase64String(assemblyDeltaReload.ILDelta),
						PdbBytes = Convert.FromBase64String(assemblyDeltaReload.PdbDelta),
						ModuleId = Guid.Parse(assemblyDeltaReload.ModuleId),
						UpdatedTypes = ReadIntArray(changedTypesReader)
					};

					_status.ConfigureSourceForNextOperation(HotReloadSource.DevServer);
					_agent?.ApplyDeltas(new[] { delta });

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Done applying IL Delta for {string.Join(",", assemblyDeltaReload.FilePaths)}, Guid:{assemblyDeltaReload.ModuleId}");
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Failed to apply IL Delta for {string.Join(",", assemblyDeltaReload.FilePaths)} ({assemblyDeltaReload})");
					}
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"An exception occurred when applying IL Delta for {string.Join(",", assemblyDeltaReload.FilePaths)} ({assemblyDeltaReload.ModuleId})", e);
				}
			}
			finally
			{
				_status.ConfigureSourceForNextOperation(default); // runtime
			}
		}

		static int[] ReadIntArray(BinaryReader binaryReader)
		{
			var numValues = binaryReader.ReadInt32();
			if (numValues == 0)
			{
				return Array.Empty<int>();
			}

			var values = new int[numValues];

			for (var i = 0; i < numValues; i++)
			{
				values[i] = binaryReader.ReadInt32();
			}

			return values;
		}

	}
}
