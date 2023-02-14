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
using Microsoft.UI.Xaml;
using WinUI = Microsoft.UI.Xaml;
using WUX = Microsoft.UI.Xaml;
using TizenWindow = ElmSharp.Window;
using Tizen.Applications;
using Uno.UI.Runtime.Skia.Tizen;
using Windows.Devices.Haptics;
using Uno.UI.Runtime.Skia.Tizen.Devices.Haptics;
using Uno.UI.Notifications;
using Uno.UI.Runtime.Skia.Tizen.UI.Notifications;
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
using Windows.System.Profile.Internal;
using System.ComponentModel;
using Windows.UI.Core;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Runtime.Skia
{
	public class TizenHost : ISkiaHost
	{
		[ThreadStatic]
		private static TizenHost? _current;

		private readonly TizenApplication _tizenApplication;
		private readonly Func<WinUI.Application> _appBuilder;
		public static TizenHost? Current => _current;

		/// <summary>
		/// Creates a host for a Uno Skia Tizen application.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <param name="args">Arguments.</param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public TizenHost(Func<WinUI.Application> appBuilder, string[]? args = null) : this(appBuilder)
		{
		}

		public TizenHost(Func<WinUI.Application> appBuilder)
		{
			Elementary.Initialize();
			Elementary.ThemeOverlay();

			_current = this;

			_appBuilder = appBuilder;

			Windows.UI.Core.CoreDispatcher.DispatchOverride = (d) => EcoreMainloop.PostAndWakeUp(d);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => EcoreMainloop.IsMainThread;

			_tizenApplication = new TizenApplication(this);
			_tizenApplication.Run(Environment.GetCommandLineArgs());
		}

		public void Run()
		{
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new TizenCoreWindowExtension(o, _tizenApplication.Canvas));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new TizenApplicationViewExtension(o, _tizenApplication.Window));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new TizenDisplayInformationExtension(o, _tizenApplication.Window));
			ApiExtensibility.Register(typeof(IVibrationDeviceExtension), o => new TizenVibrationDeviceExtension(o));
			ApiExtensibility.Register(typeof(ISimpleHapticsControllerExtension), o => new TizenSimpleHapticsControllerExtension(o));
			ApiExtensibility.Register(typeof(IBadgeUpdaterExtension), o => new TizenBadgeUpdaterExtension(o));
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

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

			WinUI.Application.StartWithArguments(CreateApp);
		}

		private void OnCoreWindowContentRootSet(object? sender, object e)
		{
			var xamlRoot = CoreServices.Instance
				.ContentRootCoordinator
				.CoreWindowContentRoot?
				.GetOrCreateXamlRoot();

			if (xamlRoot is null)
			{
				throw new InvalidOperationException("XamlRoot was not properly initialized");
			}

			xamlRoot.InvalidateRender += _tizenApplication!.Canvas.InvalidateRender;

			CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
		}
	}
}
