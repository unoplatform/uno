using System;
using Uno.UI.Runtime.Skia;

namespace UnoApp50.Skia.Gtk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppHead.InitializeLogging();

        var host = SkiaHostBuilder.Create()
            .App(() => new AppHead())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();
    }
}
