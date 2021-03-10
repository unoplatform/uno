using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Helpers.Theming;
using Uno.Logging;
using Uno.UI.Runtime.Skia.GTK.Extensions.Helpers.Theming;
using Windows.UI.Xaml;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class GtkHost : ISkiaHost
	{
		[ThreadStatic]
		private static bool _isDispatcherThread = false;

		private readonly string[] _args;
		private readonly Func<WUX.Application> _appBuilder;
		private static Gtk.Window _window;
		private UnoDrawingArea _area;
		private GtkDisplayInformationExtension _displayInformationExtension;

		public static Gtk.Window Window => _window;

		public GtkHost(Func<WUX.Application> appBuilder, string[] args)
		{
			_args = args;
			_appBuilder = appBuilder;
		}

		public void Run()
		{
			Gtk.Application.Init();			
			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new GtkCoreWindowExtension(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new GtkApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new GtkSystemThemeHelperExtension(o));
			ApiExtensibility.Register(typeof(Windows.Graphics.Display.IDisplayInformationExtension), o => _displayInformationExtension ??= new GtkDisplayInformationExtension(o, _window));

			_isDispatcherThread = true;
			_window = new Gtk.Window("Uno Host");
			_window.SetDefaultSize(1024, 800);
			_window.SetPosition(Gtk.WindowPosition.Center);

			_window.DeleteEvent += delegate
			{
				Gtk.Application.Quit();
			};

			bool EnqueueNative(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
			{
				Dispatch(() => callback());

				return true;
			}

			Windows.System.DispatcherQueue.EnqueueNativeOverride = EnqueueNative;

			void Dispatch(Action d)
			{
				if (Gtk.Application.EventsPending())
				{
					Gtk.Application.RunIteration(false);
				}

				GLib.Idle.Add(delegate
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
					{
						this.Log().Trace($"Iteration");
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
			}

			Windows.UI.Core.CoreDispatcher.DispatchOverride = Dispatch;
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;

			_window.Realized += (s, e) =>
			{
				WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(_window.AllocatedWidth, _window.AllocatedHeight));
			};

			_window.SizeAllocated += (s, e) =>
			{
				WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(e.Allocation.Width, e.Allocation.Height));
			};

			_area = new UnoDrawingArea();
			_window.Add(_area);

			/* avoids double invokes at window level */
			_area.AddEvents((int)GtkCoreWindowExtension.RequestedEvents);

			_window.ShowAll();

			void CreateApp(ApplicationInitializationCallbackParams _)
			{
				var app = _appBuilder();
				app.Host = this;
			}

			WUX.Application.Start(CreateApp, _args);

			UpdateWindowPropertiesFromPackage();

			Gtk.Application.Run();
		}

		private void UpdateWindowPropertiesFromPackage()
		{
			if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
			{
				var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
				var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, basePath);

				if (File.Exists(iconPath))
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
					{
						this.Log().Warn($"Loading icon file [{iconPath}] from Package.appxmanifest file");
					}

					GtkHost.Window.SetIconFromFile(iconPath);
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
					{
						this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
					}
				}
			}

			Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}

		public void TakeScreenshot(string filePath)
		{
			_area.TakeScreenshot(filePath);
		}
	}
}
