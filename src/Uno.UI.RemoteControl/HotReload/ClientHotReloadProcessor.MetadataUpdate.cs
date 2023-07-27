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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RemoteControl.HotReload
{
	partial class ClientHotReloadProcessor : IRemoteControlProcessor
	{
		private static int _isReloading;

		private bool _linkerEnabled;
		private HotReloadAgent _agent;
		private ElementUpdateAgent? _elementAgent;
		private static ClientHotReloadProcessor? _instance;
		private readonly TaskCompletionSource<bool> _hotreloadWorkloadSpaceLoaded = new();

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

		private void WorkspaceLoadResult(HotReloadWorkspaceLoadResult hotReloadWorkspaceLoadResult)
			=> _hotreloadWorkloadSpaceLoaded.SetResult(hotReloadWorkspaceLoadResult.WorkspaceInitialized);

		/// <summary>
		/// Determines if the server's hot reload workspace has been loaded
		/// </summary>
		internal Task HotReloadWorkspaceLoaded
			=> _hotreloadWorkloadSpaceLoaded.Task;

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

		private static async Task<bool> ShouldReload()
		{
			if (Interlocked.CompareExchange(ref _isReloading, 1, 0) == 1)
			{
				return false;
			}
			try
			{
				await TypeMappings.WaitForMappingsToResume();
			}
			finally
			{
				Interlocked.Exchange(ref _isReloading, 0);
			}
			return true;
		}

		private static async void ReloadWithUpdatedTypes(Type[] updatedTypes)
		{
			if (!await ShouldReload())
			{
				return;
			}

			try
			{

				var handlerActions = _instance?.ElementAgent?.ElementHandlerActions;

				// Action: BeforeVisualTreeUpdate
				// This is called before the visual tree is updated
				_ = handlerActions?.Do(h => h.Value.BeforeVisualTreeUpdate(updatedTypes)).ToArray();

				var capturedStates = new Dictionary<string, Dictionary<string, object>>();

				var isCapturingState = true;
				var treeIterator = EnumerateHotReloadInstances(
						Window.Current!.Content,
						(fe, key) =>
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

							if (handler is not null)
							{
								if (!capturedStates.TryGetValue(key, out var dict))
								{
									dict = new();
								}
								if (isCapturingState)
								{
									handler.CaptureState(fe, dict, updatedTypes);
									if (dict.Any())
									{
										capturedStates[key] = dict;
									}
								}
								else
								{
									handler.RestoreState(fe, dict, updatedTypes);
								}
							}

							return (handler is not null || mappedType is not null) ? (fe, handler, mappedType) : default;
						},
						parentKey: default);

				// Forced iteration to capture all state before doing ui update
				var instancesToUpdate = treeIterator.ToArray();


				// Iterate through the visual tree and either invole ElementUpdate, 
				// or replace the element with a new one
				foreach (var (element, elementHandler, elementMappedType) in instancesToUpdate)
				{
					// Action: ElementUpdate
					// This is invoked for each existing element that is in the tree that needs to be replaced
					elementHandler?.ElementUpdate(element, updatedTypes);

					if (elementMappedType is not null)
					{
						ReplaceViewInstance(element, elementMappedType, elementHandler);
					}
				}

				isCapturingState = false;
				// Forced iteration again to restore all state after doing ui update
				_ = treeIterator.ToArray();

				// Action: AfterVisualTreeUpdate
				_ = handlerActions?.Do(h => h.Value.AfterVisualTreeUpdate(updatedTypes)).ToArray();
			}
			catch (Exception ex)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error($"Error doing UI Update - {ex.Message}", ex);
				}
				throw;
			}

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
					TypeMappings.RegisterMapping(t, update.OriginalType);
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
