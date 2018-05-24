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

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		public Application()
		{
			CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, Initialize);
		}

		private void Initialize()
		{
			using (WritePhaseEventTrace(TraceProvider.LauchedStart, TraceProvider.LauchedStop))
			{
				// Force init
				Window.Current.ToString();

				Current = this;
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