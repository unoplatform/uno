#nullable enable

using System;
using Gtk;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.GTK.Extensions.Helpers;
using Windows.UI.Xaml;
using WinUIApplication = Windows.UI.Xaml.Application;
using WUX = Windows.UI.Xaml;
using GtkApplication = Gtk.Application;
using WinUIWindow = Windows.UI.Xaml.Window;
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

		internal UnoGtkWindow? MainWindow { get; private set; }

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

			SetupMainWindow();

			GtkApplication.Run();
		}

		private void InitializeDispatcher()
		{
			_isDispatcherThread = true;

			Windows.UI.Core.CoreDispatcher.DispatchOverride = DispatchNativeSingle;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
		}

		private void SetupMainWindow()
		{
			MainWindow = new UnoGtkWindow(WinUIWindow.Current);
			//WpfApplication.Current.MainWindow.Activated += MainWindow_Activated;
			//WpfApplication.Current.MainWindow.Deactivated += MainWindow_Deactivated;
			//WpfApplication.Current.MainWindow.StateChanged += MainWindow_StateChanged;
			//WpfApplication.Current.MainWindow.Closing += MainWindow_Closing;
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
