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

			var localFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
			var isHostedMode = !WebAssemblyRuntime.IsWebAssembly;
			var isLoadEventsEnabled = !FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded;
			WindowManagerInterop.Init(localFolderPath, isHostedMode, isLoadEventsEnabled);

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

				Windows.UI.Xaml.GenericStyles.Initialize();

				var arguments = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.findLaunchArguments()");

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Launch arguments: " + arguments);
				}

				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, arguments));
			}
		}
	}
}
#endif
