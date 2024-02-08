using System;
// using Uno.Foundation.Extensibility;
// using Uno.Media.Playback;

using Uno.UI.Runtime.Skia.MacOS;

namespace SkiaSharpExample;

public class MainClass
{
	[STAThread]
	public static void Main()
	{
		SamplesApp.App.ConfigureLogging(); // Enable tracing of the host

		// ApiExtensibility.Register(typeof(IMediaPlayerExtension), o => new Uno.UI.Media.MediaPlayerExtension(o));
		// ApiExtensibility.Register(typeof(IMediaPlayerPresenterExtension), o => new Uno.UI.Media.MediaPlayerPresenterExtension(o));

		AppDomain.CurrentDomain.UnhandledException += delegate (object sender, System.UnhandledExceptionEventArgs args)
		{
			Console.WriteLine($"UNHANDLED {(args.IsTerminating ? "FINAL" : "")} EXCEPTION {args.ExceptionObject}");
		};

		var host = new MacSkiaHost(() => new SamplesApp.App());
#if IS_CI
		// TODO: To be confirmed but virtualization often mess up with Metal support
		host.RenderSurfaceType = RenderSurfaceType.Software;
#endif
		host.Run();
	}
}
