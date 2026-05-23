using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI;
using Uno.UI.Tests.App.Views;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Resources;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace UnitTestsApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	sealed partial class App : Application
	{
		private Window _mainWindow;

		public Grid HostView { get; private set; }

		public App()
		{
			CustomXamlResourceLoader.Current = new MyResourceLoader();
			this.InitializeComponent();
		}

		protected
#if !NETFX_CORE
			internal
#endif
			override void OnLaunched(LaunchActivatedEventArgs args)
		{
			if (HostView == null)
			{
				HostView = new Grid() { Name = "HostView" };

				EnsureMainWindow();

				_mainWindow.Content = HostView;
				_mainWindow.Activate();
			}

			OnLaunchedPartial();
		}

		internal Window MainWindow => _mainWindow;

		partial void OnLaunchedPartial();

		/// <summary>
		/// Ensure that application exists, for unit tests. 
		/// </summary>
		/// <returns>The 'running' application.</returns>
		public static App EnsureApplication()
		{
			if (Current is not App)
			{
				// Hot reload callbacks are generated from the XAML because UnoForceHotReloadCodeGen is enabled, but here we disable
				// hot reload by default. Certain tests explicitly re-enable it.
				FeatureConfiguration.Xaml.ForceHotReloadDisabled = true;

				// The Skia Application enforces being started via Application.Start (it sets
				// the start-invoked flag and drives InitializationCompleted + OnLaunched).
				// Constructing the App directly would throw, so go through the real entry point.
				Application.Start(_ => new App());
			}

			var app = Current as App;
			app.HostView.Children.Clear();

			// Reset the simulated window to its default size each test. SetWindowSize is a
			// sticky override on the shared window, so without this a prior test's resize would
			// leak into the next test and make size-dependent behaviour order-dependent.
			app.MainWindow?.SetWindowSize(new Windows.Foundation.Size(
				Uno.UI.Xaml.Controls.NativeWindowWrapperBase.InitialWidth,
				Uno.UI.Xaml.Controls.NativeWindowWrapperBase.InitialHeight));

			// Note: the app-level custom-theme axis (ApplicationHelper.RequestedCustomTheme) was removed in
			// the WinUI theming alignment (Phase 6, D7); there is no longer a custom theme to reset here.

			return app;
		}

		[MemberNotNull(nameof(_mainWindow))]
		private void EnsureMainWindow()
		{
			if (CoreApplication.IsFullFledgedApp)
			{
				_mainWindow ??=
#if HAS_UNO_WINUI
					new Microsoft.UI.Xaml.Window();
#elif HAS_UNO
				Microsoft.UI.Xaml.Window.CurrentSafe!;
#else
				Window.Current;
#endif
			}
		}
	}
}
