#if NET6_0_OR_GREATER || __WASM__ || __SKIA__
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor : IRemoteControlProcessor
	{
		private bool _linkerEnabled;
		private HotReloadAgent _agent;
		private static ClientHotReloadProcessor? _instance;

		[MemberNotNull(nameof(_agent))]
		partial void InitializeMetadataUpdater()
		{
			_instance = this;

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

				_agent.ApplyDeltas(new[] { delta });
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Failed to apply IL Delta for {assemblyDeltaReload.FilePath} ({assemblyDeltaReload})");
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


		private static void ReloadWithUpdatedTypes(Type[] updatedTypes)
		{
			if (updatedTypes.Length == 0)
			{
				ReloadWithLastChangedFile();
				return;
			}

			foreach (var updatedType in updatedTypes)
			{
				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.LogDebug($"Processing changed type [{updatedType}]");
				}

				if (updatedType.Is<UIElement>())
				{
					ReplaceViewInstances(i => updatedType.IsInstanceOfType(i));
				}
				else
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.LogDebug($"Type [{updatedType}] is not a UIElement, skipping");
					}
				}
			}
		}

		/// <summary>
		/// Reload with the last updated file after metadata was updated
		/// </summary>
		/// <remarks>
		/// This scenario can happen when using WebAssembly from VisualStudio 2022, where changed types are not provided by browserlink.
		/// </remarks>
		private static void ReloadWithLastChangedFile()
		{
			var lastUpdated = _instance?._lastUpdatedFilePath;

			if (lastUpdated is null)
			{
				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.LogDebug($"Last changed filed is not available, skipping");
				}

				return;
			}

			if (_log.IsEnabled(LogLevel.Debug))
			{
				_log.LogDebug($"Processing last changed file [{lastUpdated}]");
			}

			var uri = new Uri("file:///" + lastUpdated.Replace('\\', '/'));

			// Search for all types in the main window's tree that
			// match the last modified uri.
			ReplaceViewInstances(i => uri.OriginalString == i.DebugParseContext?.LocalFileUri);
		}

		private static void ReplaceViewInstances(Func<FrameworkElement, bool> predicate)
		{
			foreach (var instance in EnumerateInstances(Window.Current.Content, predicate))
			{
				if (instance.GetType().GetConstructor(Array.Empty<Type>()) is { })
				{
					if (_log.IsEnabled(LogLevel.Trace))
					{
						_log.Trace($"Creating instance of type {instance.GetType()}");
					}

					var newInstance = Activator.CreateInstance(instance.GetType());

					switch (instance)
					{
#if __IOS__
						case UserControl userControl:
							if (newInstance is UIKit.UIView newUIViewContent)
							{
								SwapViews(userControl, newUIViewContent);
							}
							break;
#endif
						case ContentControl content:
							if (newInstance is ContentControl newContent)
							{
								SwapViews(content, newContent);
							}
							break;
					}
				}
				else
				{
					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.LogDebug($"Type [{instance.GetType()}] has no parameterless constructor, skipping reload");
					}
				}
			}
		}

		public static void UpdateApplication(Type[] types)
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"UpdateApplication (changed types: {string.Join(", ", types.Select(s => s.ToString()))})");
			}

			_ = Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() => ReloadWithUpdatedTypes(types));
		}
	}
}
#endif
