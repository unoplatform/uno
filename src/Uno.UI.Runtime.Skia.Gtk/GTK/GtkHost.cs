using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class GtkHost
	{
		private Func<WUX.Application> _appBuilder;
		private static Gtk.Window _window;

		public static Gtk.Window Window => _window;

		public GtkHost(Func<WUX.Application> appBuilder, string[] args)
		{
			_appBuilder = appBuilder;
		}

		public void Run()
		{
			Gtk.Application.Init();

			ApiExtensibility.Register(typeof(Windows.UI.Core.ICoreWindowExtension), o => new GtkUIElementPointersSupport(o));
			ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new GtkApplicationViewExtension(o));
			ApiExtensibility.Register(typeof(IApplicationExtension), o => new GtkApplicationExtension(o));

			_window = new Gtk.Window("Uno Host");
			_window.SetDefaultSize(1024, 800);
			_window.SetPosition(Gtk.WindowPosition.Center);

			_window.DeleteEvent += delegate
			{
				Gtk.Application.Quit();
			};

			Windows.UI.Core.CoreDispatcher.DispatchOverride
			   = d =>
			   {
				   if (Gtk.Application.EventsPending())
				   {
					   Gtk.Application.RunIteration(false);
				   }

				   GLib.Idle.Add(delegate
				   {
					   Console.WriteLine("iteration");
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
			   };

			_window.Realized += (s, e) =>
			{
				WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(_window.AllocatedWidth, _window.AllocatedHeight));
			};

			_window.SizeAllocated += (s, e) =>
			{
				WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(e.Allocation.Width, e.Allocation.Height));
			};

			var area = new UnoDrawingArea();
			_window.Add(area);

			/* avoids double invokes at window level */
			area.AddEvents((int)(
				Gdk.EventMask.PointerMotionMask
			 | Gdk.EventMask.ButtonPressMask
			 | Gdk.EventMask.ButtonReleaseMask
			));

			_window.ShowAll();

			WUX.Application.Start(_ => _appBuilder());

			Gtk.Application.Run();
		}
	}
}
