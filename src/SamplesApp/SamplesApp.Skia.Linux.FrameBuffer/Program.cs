using System;
using SkiaSharp;
using Uno.Foundation.Extensibility;
using System.Threading;
using Uno.UI.Runtime.Skia;

namespace SkiaSharpExample
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			try 
			{
				SamplesApp.App.ConfigureFilters(); // Enable tracing of the host

				Console.CursorVisible = false;
				var host = new FrameBufferHost(() => new SamplesApp.App(), args);
				host.Run();
			}
			finally
			{
				Console.CursorVisible = true;
			}
		}
	}
}
