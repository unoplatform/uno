using System;
using System.Runtime.InteropServices;
using Avalonia.X11;
using Uno.WinUI.Runtime.Skia.X11;

namespace SkiaSharpExample
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			SamplesApp.App.ConfigureLogging(); // Enable tracing of the host

			// This seems to be necessary to run on WSL, but not necessary on the X.org implementation.
			// We therefore wrap every x11 call with XLockDisplay and XUnlockDisplay
			X11Helper.XInitThreads();

			// keyboard input fails without this, not sure why this works but Avalonia and xev make similar calls.
			setlocale(/* LC_ALL */ 6, "");
			if (XLib.XSetLocaleModifiers("@im=none") == IntPtr.Zero)
			{
				setlocale(/* LC_ALL */ 6, "en_US.UTF-8");
				if (XLib.XSetLocaleModifiers("@im=none") == IntPtr.Zero)
				{
					setlocale(/* LC_ALL */ 6, "C.UTF-8");
					XLib.XSetLocaleModifiers("@im=none");
				}
			}

			var host = new X11ApplicationHost(() => new SamplesApp.App());
			host.Run();
		}

		[DllImport("libc")]
		private static extern void setlocale(int type, string s);
	}
}
