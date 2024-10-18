using System;
using GLib;
using SkiaSharp;
using Gtk;
using Uno.Foundation.Extensibility;
using System.Threading;
using Uno.UI.Runtime.Skia.Gtk;
using Uno.Media.Playback;
using Windows.UI.Xaml.Controls;

namespace SkiaSharpExample
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			SamplesApp.App.ConfigureLogging(); // Enable tracing of the host

			ApiExtensibility.Register(typeof(IMediaPlayerExtension), o => new Uno.UI.Media.MediaPlayerExtension(o));
			ApiExtensibility.Register(typeof(IMediaPlayerPresenterExtension), o => new Uno.UI.Media.MediaPlayerPresenterExtension(o));

			ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
			{
				Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
				expArgs.ExitApplication = true;
			};

			var host = new GtkHost(() => new SamplesApp.App());

#if IS_CI
			// Avoids "GL implementation doesn't support any form of non-power-of-two textures" in CI for snapshot tests when run on Windows
			host.RenderSurfaceType = RenderSurfaceType.Software;
#endif
			host.Run();
		}
	}
}
