using System;
using SkiaSharp;
using Uno.Foundation.Extensibility;
using System.Threading;
using Uno.UI.Runtime.Skia;
using Microsoft.UI.Xaml;
using Windows.UI.Core;

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
				var host = new FrameBufferHost(() =>
				{
					CoreWindow.GetForCurrentThread().KeyDown += (s, e) =>
					{
						if (e.VirtualKey == Windows.System.VirtualKey.F12)
						{
							Application.Current.Exit();
						}
					};

					return new SamplesApp.App();
				}, args);
				host.Run();
			}
			finally
			{
				Console.CursorVisible = true;
			}
		}
	}
}
