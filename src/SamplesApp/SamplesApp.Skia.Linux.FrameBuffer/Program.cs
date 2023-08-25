using System;
using SkiaSharp;
using Uno.Foundation.Extensibility;
using System.Threading;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer;
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
				SamplesApp.App.ConfigureLogging(); // Enable tracing of the host

				Console.CursorVisible = false;
				var host = new FrameBufferHost(() =>
				{
					CoreWindow.IShouldntUseGetForCurrentThread().KeyDown += (s, e) =>
					{
						if (e.VirtualKey == Windows.System.VirtualKey.F12)
						{
							Application.Current.Exit();
						}
					};

					return new SamplesApp.App();
				});
				host.Run();
			}
			finally
			{
				Console.CursorVisible = true;
			}
		}
	}
}
