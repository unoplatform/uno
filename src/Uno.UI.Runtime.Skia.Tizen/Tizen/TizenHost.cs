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
			
			Windows.UI.Core.CoreDispatcher.DispatchOverride
				= (d) => EcoreMainloop.PostAndWakeUp(d);

			_tizenApplication = new TizenApplication(this);
			_tizenApplication.Run(_args);
		}

		public void Run()
		{
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new TizenUIElementPointersSupport(o, _tizenApplication.Canvas));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new TizenApplicationViewExtension(o, _tizenApplication.Window));
			ApiExtensibility.Register(typeof(IApplicationExtension), o => new TizenApplicationExtension(o));
			ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new TizenDisplayInformationExtension(o, _tizenApplication.Window));
			
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
