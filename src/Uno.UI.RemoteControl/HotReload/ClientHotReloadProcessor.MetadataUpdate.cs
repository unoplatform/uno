#if NET6_0_OR_GREATER || __WASM__ || __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor : IRemoteControlProcessor
	{
		private ApplyUpdateHandler _applyUpdate;

		private delegate void ApplyUpdateHandler(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta);

		private void AssemblyReload(AssemblyDeltaReload assemblyDeltaReload)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().LogTrace($"Applying IL Delta after {assemblyDeltaReload.FilePath}, Guid:{assemblyDeltaReload.ModuleId}");
			}

			var moduleIdGuid = Guid.Parse(assemblyDeltaReload.ModuleId);
			var assembly = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.Modules.FirstOrDefault() is Module m && m.ModuleVersionId == moduleIdGuid);

			ReadOnlySpan<byte> metadataDelta = Convert.FromBase64String(assemblyDeltaReload.MetadataDelta);
			ReadOnlySpan<byte> ilDeta = Convert.FromBase64String(assemblyDeltaReload.ILDelta);
			ReadOnlySpan<byte> pdbDelta = Convert.FromBase64String(assemblyDeltaReload.PdbDelta);

			if (!(assembly is null))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogTrace($"Applying IL Delta for {assembly} (metadata: {metadataDelta.Length}, metadata: {metadataDelta.Length}, metadata: {metadataDelta.Length})");
				}

#if NET6_OR_GREATER
				System.Reflection.Metadata.MetadataUpdater.ApplyUpdate(assembly, metadataDelta, ilDeta, pdbDelta);
#else
				if (_applyUpdate == null)
				{
					if (Type.GetType("System.Reflection.Metadata.MetadataUpdater") is { } type)
					{
						if (type.GetMethod("ApplyUpdate") is { } applyUpdateMethod)
						{
							_applyUpdate = (ApplyUpdateHandler)applyUpdateMethod.CreateDelegate(typeof(ApplyUpdateHandler));
						}
						else
						{
							throw new NotSupportedException($"Unable to find System.Reflection.Metadata.AssemblyExtensions.ApplyUpdate(...)");
						}
					}
					else
					{
						throw new NotSupportedException($"Unable to find System.Reflection.Metadata.AssemblyExtensions");
					}
				}

				_applyUpdate(assembly, metadataDelta, ilDeta, pdbDelta);
#endif
			}
		}
	}
}
#endif
