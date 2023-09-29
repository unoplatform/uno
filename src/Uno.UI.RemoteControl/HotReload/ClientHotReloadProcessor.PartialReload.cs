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
			_supportsLightweightHotReload = (_msbuildProperties?.TryGetValue("TargetFramework", out var targetFramework) ?? false)
				&& (_msbuildProperties?.TryGetValue("BuildingInsideVisualStudio", out var buildingInsideVisualStudio) ?? false)
				&& buildingInsideVisualStudio.Equals("true", StringComparison.OrdinalIgnoreCase)
				&& (
					targetFramework.Contains("-android")
					|| targetFramework.Contains("-ios"));

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
				await Task.Delay(250);

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

					var actions = _agent.GetMetadataUpdateHandlerActions();

					actions.ClearCache.ForEach(a => a(newTypes));
					actions.UpdateApplication.ForEach(a => a(newTypes));

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"ObserveUpdateTypeMapping: Invoked metadata updaters");
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
