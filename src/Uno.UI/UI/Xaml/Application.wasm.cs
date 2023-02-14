#nullable enable

#if __WASM__
using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Threading;
using Uno.UI;
using Uno.UI.Xaml;
using Uno;
using System.Web;
using System.Collections.Specialized;
using Uno.Helpers;


#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class Application
	{
		private static bool _startInvoked;

		public Application()
		{
			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Microsoft.UI.Xaml.Application.Start(_ => new App());");
			}

			Current = this;
			Package.SetEntryAssembly(this.GetType().Assembly);

			global::Uno.Foundation.Extensibility.ApiExtensibility.Register(
				typeof(global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension),
				o => global::Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.GetForCurrentView());

			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);

			ObserveApplicationVisibility();
		}

		public static int DispatchSystemThemeChange()
		{
			Microsoft.UI.Xaml.Application.Current.OnSystemThemeChanged();
			return 0;
		}

		public static int DispatchVisibilityChange(bool isVisible)
		{
			var application = Microsoft.UI.Xaml.Application.Current;
			var window = Microsoft.UI.Xaml.Window.Current;
			if (isVisible)
			{
				application?.RaiseLeavingBackground(() =>
				{
					window?.OnVisibilityChanged(true);
					window?.OnActivated(CoreWindowActivationState.CodeActivated);
				});
			}
			else
			{
				window?.OnActivated(CoreWindowActivationState.Deactivated);
				window?.OnVisibilityChanged(false);
				application?.RaiseEnteredBackground(null);
			}

			return 0;
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			_startInvoked = true;

			var isHostedMode = !WebAssemblyRuntime.IsWebAssembly;
			var isLoadEventsEnabled = !FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded;
			WindowManagerInterop.Init(isHostedMode, isLoadEventsEnabled);
			Windows.Storage.ApplicationData.Init();

			SynchronizationContext.SetSynchronizationContext(
				new CoreDispatcherSynchronizationContext(CoreDispatcher.Main, CoreDispatcherPriority.Normal)
			);

			callback(new ApplicationInitializationCallbackParams());
		}

		partial void ObserveSystemThemeChanges()
		{
			WebAssemblyRuntime.InvokeJS("Microsoft.UI.Xaml.Application.observeSystemTheme()");
		}

		private void Initialize()
		{
			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				// Force init
				Window.Current.ToString();

				var arguments = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.findLaunchArguments()");

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Launch arguments: " + arguments);
				}
				InitializationCompleted();

				if (!string.IsNullOrEmpty(arguments))
				{
					if (ProtocolActivation.TryParseActivationUri(arguments, out var activationUri))
					{
						OnActivated(new ProtocolActivatedEventArgs(activationUri, ApplicationExecutionState.NotRunning));
						return;
					}
				}

				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, arguments));
			}
		}

		/// <summary>
		/// Dispatch method from Javascript
		/// </summary>
		internal static void DispatchSuspending()
		{
			Current?.RaiseSuspending();
		}

		private void ObserveApplicationVisibility()
		{
			WebAssemblyRuntime.InvokeJS("Microsoft.UI.Xaml.Application.observeVisibility()");
		}
	}
}
#endif
