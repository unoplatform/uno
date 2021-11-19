#if NET6_0_OR_GREATER || __WASM__ || __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor : IRemoteControlProcessor
	{
		private const string MetadataUpdaterType = "System.Reflection.Metadata.MetadataUpdater";

		private ApplyUpdateHandler _applyUpdate;
		private bool _linkerEnabled;

		private delegate void ApplyUpdateHandler(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta);

		partial void InitializeMetadataUpdater()
		{
			_linkerEnabled = string.Equals(Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_LINKER_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);

			if (_linkerEnabled)
			{
				var message = "The application was compiled with the IL linker enabled, hot reload is disabled. " +
							"See WasmShellILLinkerEnabled for more details.";

				Console.WriteLine($"[ERROR] {message}");
			}
		}

		private string[] GetMetadataUpdateCapabilities()
		{
			if (Type.GetType(MetadataUpdaterType) is { } type)
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
							this.Log().Trace($"Runtime does not support Hot Reload (Invalid returned type for {MetadataUpdaterType}.GetCapabilities())");
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Trace($"Runtime does not support Hot Reload (Unable to find method {MetadataUpdaterType}.GetCapabilities())");
					}
				}
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Trace($"Runtime does not support Hot Reload (Unable to find type {MetadataUpdaterType})");
				}
			}
			return Array.Empty<string>();
		}

		private void AssemblyReload(AssemblyDeltaReload assemblyDeltaReload)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Applying IL Delta after {assemblyDeltaReload.FilePath}, Guid:{assemblyDeltaReload.ModuleId}");
			}

			var moduleIdGuid = Guid.Parse(assemblyDeltaReload.ModuleId);
			var assemblyQuery = from a in AppDomain.CurrentDomain.GetAssemblies()
								from m in a.Modules
								where m.ModuleVersionId == moduleIdGuid
								select a;

			var assembly = assemblyQuery.FirstOrDefault();

			ReadOnlySpan<byte> metadataDelta = Convert.FromBase64String(assemblyDeltaReload.MetadataDelta);
			ReadOnlySpan<byte> ilDeta = Convert.FromBase64String(assemblyDeltaReload.ILDelta);
			ReadOnlySpan<byte> pdbDelta = Convert.FromBase64String(assemblyDeltaReload.PdbDelta);

			if (assembly is not null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Trace($"Applying IL Delta for {assembly} (metadata: {metadataDelta.Length}, metadata: {metadataDelta.Length}, metadata: {metadataDelta.Length})");
				}

#if NET6_OR_GREATER
				System.Reflection.Metadata.MetadataUpdater.ApplyUpdate(assembly, metadataDelta, ilDeta, pdbDelta);
#else
				if (_applyUpdate == null)
				{
					if (Type.GetType(MetadataUpdaterType) is { } type)
					{
						if (type.GetMethod("ApplyUpdate") is { } applyUpdateMethod)
						{
							_applyUpdate = (ApplyUpdateHandler)applyUpdateMethod.CreateDelegate(typeof(ApplyUpdateHandler));
						}
						else
						{
							throw new NotSupportedException($"Unable to find System.Reflection.Metadata.MetadataUpdater.ApplyUpdate(...)");
						}
					}
					else
					{
						throw new NotSupportedException($"Unable to find System.Reflection.Metadata.MetadataUpdater");
					}
				}

				if (_applyUpdate is not null)
				{
					_applyUpdate(assembly, metadataDelta, ilDeta, pdbDelta);
				}
#endif
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Unable to applying IL delta for {assemblyDeltaReload.FilePath} (Unable to find module with guid:{assemblyDeltaReload.ModuleId}, is the IL Linker enabled?)");
				}
			}
		}
	}
}
#endif
