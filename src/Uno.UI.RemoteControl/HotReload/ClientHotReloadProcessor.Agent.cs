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
using Microsoft/* UWP don't rename */.UI.Xaml;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using System.Diagnostics;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor
	{
		private bool _linkerEnabled;
		private HotReloadAgent? _agent;
		private bool _serverMetadataUpdatesEnabled;
		private static ClientHotReloadProcessor? _instance;
		private readonly TaskCompletionSource<bool> _hotReloadWorkloadSpaceLoaded = new();

		private void WorkspaceLoadResult(HotReloadWorkspaceLoadResult hotReloadWorkspaceLoadResult)
				=> _hotReloadWorkloadSpaceLoaded.SetResult(hotReloadWorkspaceLoadResult.WorkspaceInitialized);

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

			_serverMetadataUpdatesEnabled = BuildServerMetadataUpdatesEnabled();

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

		private bool BuildServerMetadataUpdatesEnabled()
		{
			var unoRuntimeIdentifier = GetMSBuildProperty("UnoRuntimeIdentifier");
			//var targetFramework = GetMSBuildProperty("TargetFramework");
			var buildingInsideVisualStudio = GetMSBuildProperty("BuildingInsideVisualStudio").Equals("true", StringComparison.OrdinalIgnoreCase);

			var enabled = (_forcedHotReloadMode is HotReloadMode.MetadataUpdates or HotReloadMode.Partial)

				// CoreCLR Debugger under VS Win already handles metadata updates
				// Debugger under VS Code prevents metadata based hot reload
				|| (!Debugger.IsAttached && !buildingInsideVisualStudio && unoRuntimeIdentifier.Equals("skia", StringComparison.OrdinalIgnoreCase))

				// Mono Debugger under VS Win already handles metadata updates
				// Mono Debugger under VS Code prevents metadata based hot reload
				|| (!Debugger.IsAttached && !buildingInsideVisualStudio && unoRuntimeIdentifier.Equals("webassembly", StringComparison.OrdinalIgnoreCase))

				// Disabled until https://github.com/dotnet/runtime/issues/93860 is fixed
				//
				//||
				//(
				//	buildingInsideVisualStudio.Equals("true", StringComparison.OrdinalIgnoreCase)
				//	&& (
				//		// As of VS 17.8, when the debugger is not attached, mobile targets can use
				//		// DevServer's hotreload workspace, as visual studio does not enable it on its own.
				//		(!Debugger.IsAttached
				//			&& (targetFramework.Contains("-android") || targetFramework.Contains("-ios")))))
				;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ServerMetadataUpdates Enabled:{enabled} DebuggerAttached:{Debugger.IsAttached} BuildingInsideVS: {buildingInsideVisualStudio} unorid: {unoRuntimeIdentifier}");
			}

			return enabled;
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

		private void AssemblyReload(AssemblyDeltaReload assemblyDeltaReload)
		{
			try
			{
				if (Debugger.IsAttached)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Hot Reload is not supported when the debugger is attached.");
					}

					return;
				}

				if (assemblyDeltaReload.IsValid())
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Applying IL Delta after {assemblyDeltaReload.FilePath}, Guid:{assemblyDeltaReload.ModuleId}");
					}

					var changedTypesStreams = new MemoryStream(Convert.FromBase64String(assemblyDeltaReload.UpdatedTypes));
					var changedTypesReader = new BinaryReader(changedTypesStreams);

					var delta = new UpdateDelta()
					{
						MetadataDelta = Convert.FromBase64String(assemblyDeltaReload.MetadataDelta),
						ILDelta = Convert.FromBase64String(assemblyDeltaReload.ILDelta),
						PdbBytes = Convert.FromBase64String(assemblyDeltaReload.PdbDelta),
						ModuleId = Guid.Parse(assemblyDeltaReload.ModuleId),
						UpdatedTypes = ReadIntArray(changedTypesReader)
					};

					_agent?.ApplyDeltas(new[] { delta });

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Done applying IL Delta for {assemblyDeltaReload.FilePath}, Guid:{assemblyDeltaReload.ModuleId}");
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Failed to apply IL Delta for {assemblyDeltaReload.FilePath} ({assemblyDeltaReload})");
					}
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"An exception occurred when applying IL Delta for {assemblyDeltaReload.FilePath} ({assemblyDeltaReload.ModuleId})", e);
				}
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
