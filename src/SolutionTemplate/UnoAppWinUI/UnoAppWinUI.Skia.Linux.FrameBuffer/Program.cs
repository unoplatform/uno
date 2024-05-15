using Microsoft.UI.Xaml;
using System;
using Uno.UI.Runtime.Skia.Linux.FrameBuffer;
using Windows.UI.Core;

namespace UnoAppWinUI
{
	public sealed class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Console.CursorVisible = false;

				var host = new FrameBufferHost(() =>
				{
					// Framebuffer applications don't have a WindowManager to rely
					// on. To close the application, we can hook onto CoreWindow events
					// which dispatch keyboard input, and close the application as a result.
					// This block can be moved to App.xaml.cs if it does not interfere with other
					// platforms that may use the same keys.
					CoreWindow.GetForCurrentThread().KeyDown += (s, e) =>
					{
						if (e.VirtualKey == Windows.System.VirtualKey.F12)
						{
							Application.Current.Exit();
						}
					};

					return new AppHead();
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
