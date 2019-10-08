#if __WASM__
using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Foundation;
using Uno.Extensions;
using Uno.Logging;
using System.Threading;
using Uno.UI;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		private static bool _startInvoked = false;

		public Application()
		{
			if (!_startInvoked)
			{
				throw new InvalidOperationException("The application must be started using Application.Start first, e.g. Windows.UI.Xaml.Application.Start(_ => new App());");
			}

			CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);
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


		private void Initialize()
		{
			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				Current = this;

				// Force init
				Window.Current.ToString();

				var arguments = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.findLaunchArguments()");

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Launch arguments: " + arguments);
				}
				InitializationCompleted();
				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, arguments));
			}
		}

		private ApplicationTheme GetDefaultSystemTheme()
		{
			var serializedTheme = WebAssemblyRuntime.InvokeJS("Windows.UI.Xaml.Application.getDefaultSystemTheme()");
			
			if (serializedTheme != null)
			{
				if (Enum.TryParse(serializedTheme, out ApplicationTheme theme))
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
					{
						this.Log().Info("Setting OS preferred theme: " + theme);
					}
					return theme;
				}
				else
				{
					throw new InvalidOperationException($"{serializedTheme} theme is not a supported OS theme");
				}
			}
			//OS has no preference or API not implemented, use light as default
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				this.Log().Info("No preferred theme, using Light instead");
			}
			return ApplicationTheme.Light;
		}
	}
}
#endif
