#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElmSharp;
using SkiaSharp;
using SkiaSharp.Views.Tizen;
using Tizen.NUI;
using Uno.Foundation.Extensibility;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using WinUI = Windows.UI.Xaml;
using WUX = Windows.UI.Xaml;
using TizenWindow = ElmSharp.Window;
using Tizen.Applications;
using Uno.UI.Runtime.Skia.Tizen;
using Windows.Devices.Haptics;
using Uno.UI.Runtime.Skia.Tizen.Devices.Haptics;
using Windows.System.Profile;
using Uno.UI.Runtime.Skia.Tizen.System.Profile;
using Windows.ApplicationModel;
using Windows.System;
using Uno.UI.Runtime.Skia.Tizen.ApplicationModel;
using Uno.ApplicationModel;
using Windows.ApplicationModel.Contacts;
using Uno.UI.Runtime.Skia.Tizen.ApplicationModel.Contacts;
using Uno.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.Tizen.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.Tizen.System;
using Uno.Extensions.System;

namespace Uno.UI.Runtime.Skia
{
	public class TizenHost : ISkiaHost
	{
		[ThreadStatic]
		private static TizenHost _current;

		private readonly TizenApplication _tizenApplication;
		private readonly Func<WinUI.Application> _appBuilder;
		private readonly TizenWindow _window;
		private readonly string[] _args;

		public static TizenHost Current => _current;

		/// <summary>
		/// Creates a WpfHost element to host a Uno-Skia into a WPF application.
		/// </summary>
		/// <remarks>
		/// If args are omitted, those from Environment.GetCommandLineArgs() will be used.
		/// </remarks>
		public TizenHost(Func<WinUI.Application> appBuilder, string[] args = null)
		{
			Elementary.Initialize();
			Elementary.ThemeOverlay();

			_current = this;

			_appBuilder = appBuilder;
			_args = args;


			_args ??= Environment
				.GetCommandLineArgs()
				.Skip(1)
				.ToArray();

			bool EnqueueNative(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
			{
				EcoreMainloop.PostAndWakeUp(() => callback());

				return true;
			}

			Windows.System.DispatcherQueue.EnqueueNativeOverride = EnqueueNative;

			Windows.UI.Core.CoreDispatcher.DispatchOverride = (d) => EcoreMainloop.PostAndWakeUp(d);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => EcoreMainloop.IsMainThread;

			_tizenApplication = new TizenApplication(this);
			_tizenApplication.Run(_args);
		}

		public void Run()
		{
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new TizenCoreWindowExtension(o, _tizenApplication.Canvas));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new TizenApplicationViewExtension(o, _tizenApplication.Window));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new TizenDisplayInformationExtension(o, _tizenApplication.Window));
			ApiExtensibility.Register(typeof(IVibrationDeviceExtension), o => new TizenVibrationDeviceExtension(o));
			ApiExtensibility.Register(typeof(ISimpleHapticsControllerExtension), o => new TizenSimpleHapticsControllerExtension(o));
			ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new TizenAnalyticsInfoExtension(o));
			ApiExtensibility.Register(typeof(IPackageIdExtension), o => new TizenPackageIdExtension(o));
			ApiExtensibility.Register(typeof(IDataTransferManagerExtension), o => new TizenDataTransferManagerExtension(o));
			ApiExtensibility.Register(typeof(IContactPickerExtension), o => new TizenContactPickerExtension(o));
			ApiExtensibility.Register(typeof(ILauncherExtension), o => new TizenLauncherExtension(o));

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			WUX.Window.Current.OnNativeSizeChanged(
				new Windows.Foundation.Size(
					_tizenApplication.Window.ScreenSize.Width,
					_tizenApplication.Window.ScreenSize.Height));
			WinUI.Application.Start(CreateApp, _args);
		}
	}
}
