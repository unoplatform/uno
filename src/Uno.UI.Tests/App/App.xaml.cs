using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI;
using Uno.UI.Tests.App.Views;
using Windows.ApplicationModel.Activation;
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
using LaunchActivatedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.LaunchActivatedEventArgs;
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

				var application = new App();
#if !NETFX_CORE
				application.InitializationCompleted();
#endif
				application.OnLaunched(null);
			}

			var app = Current as App;
			app.HostView.Children.Clear();

#if !NETFX_CORE
			//Clear custom theme
			Uno.UI.ApplicationHelper.RequestedCustomTheme = null;
#endif

			return app;
		}

		[MemberNotNull(nameof(_mainWindow))]
		private void EnsureMainWindow()
		{
			_mainWindow ??=
#if HAS_UNO_WINUI || WINUI_WINDOWING
				new Microsoft.UI.Xaml.Window();
#elif HAS_UNO
				Microsoft.UI.Xaml.Window.CurrentSafe!;
#else
				Window.Current;
#endif
		}
	}
}
