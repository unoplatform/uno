#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor
	{
		private Dictionary<string, Type>? _mappedTypes;

		private bool _supportsLightweightHotReload;

		private Task? _updatingTypes;
		private readonly object _updatingTypesGate = new();

		private void InitializePartialReload()
		{
			var unoRuntimeIdentifier = GetMSBuildProperty("UnoRuntimeIdentifier");
			var targetFramework = GetMSBuildProperty("TargetFramework");
			var buildingInsideVisualStudio = GetMSBuildProperty("BuildingInsideVisualStudio");

			_supportsLightweightHotReload =
				buildingInsideVisualStudio.Equals("true", StringComparison.OrdinalIgnoreCase)
				&& (_forcedHotReloadMode is null || _forcedHotReloadMode == HotReloadMode.Partial)
				&& (
					// As of VS 17.8, when the debugger is attached, mobile targets don't invoke MetadataUpdateHandlers
					// and both targets are not providing updated types. We simulate parts of this process
					// to determine which types have been updated, particularly those with "CreateNewOnMetadataUpdate".
					//
					// Disabled until https://github.com/dotnet/runtime/issues/93860 is fixed
					//
					(Debugger.IsAttached
						&& IsIssue93860Fixed()
						&& (targetFramework.Contains("-android")
							|| targetFramework.Contains("-ios")))

					// WebAssembly does not support sending updated types, and does not support debugger based hot reload.
					|| (unoRuntimeIdentifier?.Equals("WebAssembly", StringComparison.OrdinalIgnoreCase) ?? false));

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Partial Hot Reload Enabled:{_supportsLightweightHotReload} " +
					$"unoRuntimeIdentifier:{unoRuntimeIdentifier} " +
					$"targetFramework:{targetFramework} " +
					$"buildingInsideVisualStudio:{targetFramework} " +
					$"debuggerAttached:{Debugger.IsAttached} " +
					$"IsIssue93860Fixed:{IsIssue93860Fixed()}");
			}

			_mappedTypes = _supportsLightweightHotReload
				? BuildMappedTypes()
				: new();
		}

		private async Task PartialReload(FileReload fileReload)
		{
			if (!_supportsLightweightHotReload)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Skipping file reload");
				}

				return;
			}

			if (!fileReload.IsValid())
			{
				if (fileReload.FilePath is not null && this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"FileReload is missing a file path");
				}

				if (fileReload.Content is null && this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"FileReload is missing content");
				}

				return;
			}

			lock (_updatingTypesGate)
			{
				if (_updatingTypes == null || _updatingTypes.Status is TaskStatus.RanToCompletion or TaskStatus.Faulted)
				{
					_updatingTypes = ObserveUpdateTypeMapping();
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"PartialReload: Waiting for existing type observer");
					}
				}
			}

			await _updatingTypes;
		}

		private async Task ObserveUpdateTypeMapping()
		{
			var originalMappedTypes = _mappedTypes ?? new();
			var sw = Stopwatch.StartNew();

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ObserveUpdateTypeMapping: Start observing (Original mapped types {originalMappedTypes.Count})");
			}

			while (sw.Elapsed < TimeSpan.FromSeconds(15))
			{
				// Arbitrary delay to wait for VS to push updates to the app
				// so we can discover which types have changed
				// The scanning operation can take 500ms under wasm, keep the app
				// running for longer to ensure we don't miss any updates
#if __WASM__
				await Task.Delay(1000);
#else
				await Task.Delay(250);
#endif

				var mappedSw = Stopwatch.StartNew();

				// Lookup for types marked with MetadataUpdateOriginalTypeAttribute
				var mappedTypes = BuildMappedTypes();

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"ObserveUpdateTypeMapping: fetched mapped types {mappedTypes.Count} in {mappedSw.Elapsed}");
				}

				if (!mappedTypes.Values.All(b => originalMappedTypes.ContainsValue(b)))
				{
					_mappedTypes = mappedTypes;

					var newTypes = mappedTypes
						.Values
						.Except(originalMappedTypes.Values)
						.ToArray();

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						var types = string.Join(", ", _mappedTypes.Values);

						this.Log().Trace($"Found {newTypes.Length} updated types ({types})");
					}

					if (_agent is not null)
					{
						var actions = _agent.GetMetadataUpdateHandlerActions();

						actions.ClearCache.ForEach(a => a(newTypes));
						actions.UpdateApplication.ForEach(a => a(newTypes));

						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"ObserveUpdateTypeMapping: Invoked metadata updaters");
						}
					}

					return;
				}
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ObserveUpdateTypeMapping: Stopped observing after timeout");
			}
		}

		private Dictionary<string, Type> BuildMappedTypes()
		{
			var mappedTypes =
					from asm in AssemblyLoadContext.Default.Assemblies
					let debuggableAttribute = asm.GetCustomAttribute<DebuggableAttribute>()
					where debuggableAttribute is not null
						&& (debuggableAttribute.DebuggingFlags & DebuggableAttribute.DebuggingModes.DisableOptimizations) != 0
					from type in asm.GetTypes()
					let originalType = type.GetCustomAttribute<MetadataUpdateOriginalTypeAttribute>()
					where originalType is not null
					group type by originalType.OriginalType into g
					select new
					{
						Key = g.Key.FullName,
						Type = g.Key,
						LastMapped = g.OrderBy(t => t.FullName).Last()
					};

			return mappedTypes.ToDictionary(p => p.Key, p => p.LastMapped);
		}
	}
}
