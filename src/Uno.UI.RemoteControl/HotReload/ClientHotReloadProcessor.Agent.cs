
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
		private readonly TaskCompletionSource<bool> _hotReloadWorkloadSpaceLoaded = new();

		private void ProcessWorkspaceLoadResult(HotReloadWorkspaceLoadResult hotReloadWorkspaceLoadResult)
		{
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
			var runtime = OperatingSystem.IsBrowser() ? "wasm"
				: OperatingSystem.IsAndroid() ? "android"
				: OperatingSystem.IsIOS() ? "ios"
				: "desktop";

			var ide = GetMSBuildProperty("BuildingInsideVisualStudio").Equals("true", StringComparison.OrdinalIgnoreCase) // Legacy support, VS's VSIX now includes the UnoPlatformIDE
				? "visualstudio"
				: GetMSBuildProperty("UnoPlatformIDE").ToLowerInvariant();

			// This is only set when Uno's mono debugger is used inside VS Code
			var unoDebuggerSupportsHotReload = Environment.GetEnvironmentVariable("__UNO_SUPPORT_DEBUG_HOT_RELOAD__") == "true";

			var support = (ide, runtime, Debugger.IsAttached) switch
			{
				("visualstudio", "desktop", _) => MetadataUpdatesSupport.Ide | MetadataUpdatesSupport.Runtime,
				("visualstudio", "wasm", _) => MetadataUpdatesSupport.Ide | MetadataUpdatesSupport.Runtime,
				("visualstudio", "android", true) => MetadataUpdatesSupport.Ide | MetadataUpdatesSupport.Runtime,
				("visualstudio", "ios", true) => MetadataUpdatesSupport.Ide | MetadataUpdatesSupport.Runtime,

				("vscode", "desktop", _) => MetadataUpdatesSupport.Ide | MetadataUpdatesSupport.Runtime,
				("vscode", "wasm", false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl, // TODO: review support
				("vscode", "android", false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl,
				("vscode", "android", true) when unoDebuggerSupportsHotReload => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.Debugger,
				("vscode", "ios", false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl,
				("vscode", "ios", true) when unoDebuggerSupportsHotReload => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.Debugger,

				("rider", "desktop", false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl,
				("rider", "wasm", false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl,
				("rider", "android", false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl,
				("rider", "ios", false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl,

				// Hot-reload in runtime test / legacy when built in command line
				(null or "", _, false) => MetadataUpdatesSupport.DevServer | MetadataUpdatesSupport.RemoteControl,

				_ => MetadataUpdatesSupport.None
			};

			if (_forcedHotReloadMode is HotReloadMode.MetadataUpdates) // Legacy
			{
				support = MetadataUpdatesSupport.Ide | MetadataUpdatesSupport.Runtime;
			}

			_supportsMetadataUpdates = support;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"MetadataUpdates support:{support} (IDE: {ide} | runtime: {runtime} | debugger:{Debugger.IsAttached} | uno_debug_hr: {unoDebuggerSupportsHotReload})");
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
				if (!_supportsMetadataUpdates.HasFlag(MetadataUpdatesSupport.RemoteControl))
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("Hot Reload through app-chanel is not supported for the current configuration.");
					}
					_status.ReportLocalStarting([]).ReportIgnored("Hot Reload is not supported");

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
