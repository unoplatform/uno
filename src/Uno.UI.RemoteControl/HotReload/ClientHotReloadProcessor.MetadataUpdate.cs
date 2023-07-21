using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using System.Threading;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor : IRemoteControlProcessor
	{
		private bool _linkerEnabled;
		private HotReloadAgent _agent;
		private ElementUpdateAgent? _elementAgent;
		private static ClientHotReloadProcessor? _instance;

		private ElementUpdateAgent ElementAgent
		{
			get
			{
				_elementAgent ??= new ElementUpdateAgent(s =>
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace(s);
						}
					});

				return _elementAgent;
			}
		}

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

#if __WASM__ || __SKIA__
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
#endif

		private static void ReloadWithUpdatedTypes(Type[] updatedTypes)
		{
			if (updatedTypes.Length == 0)
			{
				return;
			}

			var handlerActions = _instance?.ElementAgent?.ElementHandlerActions;

			// Action: BeforeVisualTreeUpdate
			// This is called before the visual tree is updated
			handlerActions?.Do(h => h.Value.BeforeVisualTreeUpdate(updatedTypes));

			// Iterate through the visual tree and either invole ElementUpdate, 
			// or replace the element with a new one
			foreach (
				var (element, elementHandler, elementMappedType) in
				EnumerateHotReloadInstances(
					Window.Current.Content,
					fe =>
						{
							// Get the original type of the element, in case it's been replaced
							var originalType = fe.GetType().GetOriginalType() ?? fe.GetType();

							// Get the handler for the type specified
							var handler = (from h in handlerActions
										   where originalType == h.Key ||
												originalType.IsSubclassOf(h.Key)
										   select h.Value).FirstOrDefault();

							// Get the replacement type, or null if not replaced
							var mappedType = originalType.GetMappedType();

							return (handler is not null || mappedType is not null) ? (fe, handler, mappedType) : default;
						},
					enumerateChildrenAfterMatch: true))
			{

				// Action: ElementUpdate
				// This is invoked for each existing element that is in the tree that needs to be replaced
				elementHandler?.ElementUpdate(element, updatedTypes);

				if (elementMappedType is not null)
				{
					ReplaceViewInstance(element, elementMappedType, elementHandler);
				}
			}

			// Action: AfterVisualTreeUpdate
			handlerActions?.Do(h => h.Value.AfterVisualTreeUpdate(updatedTypes));
		}

		private static void ReplaceViewInstance(UIElement instance, Type replacementType, ElementUpdateAgent.ElementUpdateHandlerActions? handler = default, Type[]? updatedTypes = default)
		{
			if (replacementType.GetConstructor(Array.Empty<Type>()) is { } creator)
			{
				if (_log.IsEnabled(LogLevel.Trace))
				{
					_log.Trace($"Creating instance of type {instance.GetType()}");
				}

				var newInstance = Activator.CreateInstance(replacementType);
				var instanceFE = instance as FrameworkElement;
				var newInstanceFE = newInstance as FrameworkElement;
				if (instanceFE is not null &&
					newInstanceFE is not null)
				{
					handler?.BeforeElementReplaced(instanceFE, newInstanceFE, updatedTypes);
				}
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

				if (instanceFE is not null &&
					newInstanceFE is not null)
				{
					handler?.AfterElementReplaced(instanceFE, newInstanceFE, updatedTypes);
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

		public static void UpdateApplication(Type[] types)
		{
			foreach (var t in types)
			{
				if (t.GetCustomAttribute<System.Runtime.CompilerServices.MetadataUpdateOriginalTypeAttribute>() is { } update)
				{
					TypeMappingHelper.RegisterMapping(t, update.OriginalType);
				}
			}

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
