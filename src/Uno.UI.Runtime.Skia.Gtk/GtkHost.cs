#nullable enable

using System;
using System.IO;
using Gtk;
using Uno.ApplicationModel.DataTransfer;
using Uno.Extensions;
using Uno.Extensions.Storage.Pickers;
using Uno.Extensions.System;
using Uno.Extensions.UI.Core.Preview;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Core.Preview;
using Uno.UI.Runtime.Skia.GTK.Extensions.ApplicationModel.DataTransfer;
using Uno.UI.Runtime.Skia.GTK.Extensions.Helpers;
using Uno.UI.Runtime.Skia.GTK.Extensions.Helpers.Theming;
using Uno.UI.Runtime.Skia.GTK.Extensions.System;
using Uno.UI.Runtime.Skia.GTK.Extensions.UI.Xaml.Controls;
using Uno.UI.Runtime.Skia.GTK.System.Profile;
using Uno.UI.Runtime.Skia.GTK.UI.Core;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUIApplication = Windows.UI.Xaml.Application;
using WUX = Windows.UI.Xaml;
using Uno.UI.Xaml.Core;
using System.ComponentModel;
using Uno.Disposables;
using System.Collections.Generic;
using Uno.Extensions.ApplicationModel.Core;
using Windows.ApplicationModel;
using Uno.UI.XamlHost.Skia.Gtk.Hosting;
using System.Runtime.InteropServices;
using System.Reflection;
using Gdk;
using System.Linq;
using Size = Windows.Foundation.Size;
using GtkApplication = Gtk.Application;
using GtkWindow = Gtk.Window;
using Uno.UI.Runtime.Skia.GTK.Extensions;
using Uno.UI.Runtime.Skia.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia
{
	public partial class GtkHost : ISkiaApplicationHost
	{
		private const int UnoThemePriority = 800;

		private readonly Func<WUX.Application> _appBuilder;

		[ThreadStatic] private static bool _isDispatcherThread;
		[ThreadStatic] private static GtkHost? _current;

		static GtkHost() => GtkExtensionsRegistrar.Register();

		/// <summary>
		/// Creates a host for a Uno Skia GTK application.
		/// </summary>
		/// <param name="appBuilder">App builder.</param>
		/// <remarks>
		/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
		/// </remarks>
		public GtkHost(Func<WinUIApplication> appBuilder)
		{
			_current = this;
			_appBuilder = appBuilder;
		}

		internal static GtkHost? Current => _current;

		/// <summary>
		/// Gets or sets the current Skia Render surface type.
		/// </summary>
		/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
		public RenderSurfaceType? RenderSurfaceType { get; set; }

		public void Run()
		{
			PreloadHarfBuzz();

			if (!InitializeGtk())
			{
				return;
			}

			SetupTheme();

			InitializeDispatcher();

			StartApp();

			GtkApplication.Run();
		}

		private void InitializeDispatcher()
		{
			_isDispatcherThread = true;

			Windows.UI.Core.CoreDispatcher.DispatchOverride = DispatchNativeSingle;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
		}

		private void StartApp()
		{
			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			WinUIApplication.StartWithArguments(CreateApp);
		}

		private bool InitializeGtk()
		{
			try
			{
				Gtk.Application.Init();
				return true;
			}
			catch (TypeInitializationException e)
			{
				Console.Error.WriteLine("Unable to initialize Gtk, visit https://aka.platform.uno/gtk-install for more information.", e);
				return false;
			}
		}

		private void DispatchNativeSingle(System.Action d)
			=> GLib.Idle.Add(delegate
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Dispatch Iteration");
				}

				try
				{
					d();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				return false;
			});

		private void SetupTheme()
		{
			var cssProvider = new CssProvider();
			cssProvider.LoadFromEmbeddedResource("Theming.UnoGtk.css");
			StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, UnoThemePriority);
		}
	}
}
