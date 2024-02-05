using System;
using Uno.WinUI.Runtime.Skia.X11;

namespace SkiaSharpExample
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			// WARNING: don't make any X11 calls until X11ApplicationHost is instantiated.

			SamplesApp.App.ConfigureLogging(); // Enable tracing of the host

			var host = new X11ApplicationHost(() => new SamplesApp.App());
			host.Run();
		}
	}
}
