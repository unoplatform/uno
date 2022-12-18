#nullable enable

using System;
using System.ComponentModel;
using Tizen.Pims.Calendar.CalendarViews;
using Uno.ApplicationModel;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.UI.Notifications;
using Uno.UI.Runtime.Skia.Tizen;
using Uno.UI.Runtime.Skia.Tizen.ApplicationModel;
using Uno.UI.Runtime.Skia.Tizen.ApplicationModel.Contacts;
using Uno.UI.Runtime.Skia.Tizen.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.Tizen.Devices.Haptics;
using Uno.UI.Runtime.Skia.Tizen.System;
using Uno.UI.Runtime.Skia.Tizen.System.Profile;
using Uno.UI.Runtime.Skia.Tizen.UI.Notifications;
using Uno.UI.Xaml.Core;
using Windows.ApplicationModel.Contacts;
using Windows.Devices.Haptics;
using Windows.Graphics.Display;
using Windows.System.Profile.Internal;
using Windows.UI.Xaml;
using WinUI = Windows.UI.Xaml;
using WUX = Windows.UI.Xaml;

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
			//Elementary.Initialize();
			//Elementary.ThemeOverlay();

			_current = this;

			_appBuilder = appBuilder;

			_tizenApplication = new TizenApplication(this);
		}

		public void Run()
		{
			_tizenApplication.Run(Array.Empty<string>());//TODO:MZ:Environment.GetCommandLineArgs());
		}

		public void PostRun()
		{
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new TizenCoreWindowExtension(o, _tizenApplication.Canvas));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new TizenApplicationViewExtension(o, _tizenApplication.AppWindow));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new TizenDisplayInformationExtension(o, _tizenApplication.AppWindow));
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
