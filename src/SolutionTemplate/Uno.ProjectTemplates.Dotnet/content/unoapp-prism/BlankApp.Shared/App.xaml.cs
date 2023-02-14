using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
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
using Prism.Modularity;
using Prism;
using Prism.Ioc;
using BlankApp.Views;

namespace BlankApp
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App
	{
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			ConfigureFilters(global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);

			this.InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			base.OnLaunched(e);
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		protected override void OnSuspending(SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			//TODO: Save application state and stop any background activity
			deferral.Complete();
		}

		protected override UIElement CreateShell()
		{
			return Container.Resolve<Shell>();
		}

		protected override void RegisterTypes(IContainerRegistry containerRegistry)
		{
		}

		/// <summary>
		/// Configures global logging
		/// </summary>
		/// <param name="factory"></param>
		static void ConfigureFilters(ILoggerFactory factory)
		{
			factory
				.WithFilter(new FilterLoggerSettings
					{
						{ "Uno", LogLevel.Warning },
						{ "Windows", LogLevel.Warning },

						// Debug JS interop
						// { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },

						// Generic Xaml events
						// { "Microsoft.UI.Xaml", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.UIElement", LogLevel.Debug },

						// Layouter specific messages
						// { "Microsoft.UI.Xaml.Controls", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug },
						// { "Windows.Storage", LogLevel.Debug },

						// Binding related messages
						// { "Microsoft.UI.Xaml.Data", LogLevel.Debug },

						// DependencyObject memory references tracking
						// { "ReferenceHolder", LogLevel.Debug },

						// ListView-related messages
						// { "Microsoft.UI.Xaml.Controls.ListViewBase", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.ListView", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.GridView", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.VirtualizingPanelLayout", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.NativeListViewBase", LogLevel.Debug },
						// { "Microsoft.UI.Xaml.Controls.ListViewBaseSource", LogLevel.Debug }, //iOS
						// { "Microsoft.UI.Xaml.Controls.ListViewBaseInternalContainer", LogLevel.Debug }, //iOS
						// { "Microsoft.UI.Xaml.Controls.NativeListViewBaseAdapter", LogLevel.Debug }, //Android
						// { "Microsoft.UI.Xaml.Controls.BufferViewCache", LogLevel.Debug }, //Android
						// { "Microsoft.UI.Xaml.Controls.VirtualizingPanelGenerator", LogLevel.Debug }, //WASM
					}
				)
#if DEBUG
				.AddConsole(LogLevel.Debug);
#else
				.AddConsole(LogLevel.Information);
#endif
		}
	}
}
