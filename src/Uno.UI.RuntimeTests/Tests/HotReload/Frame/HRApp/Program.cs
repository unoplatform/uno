using System;
using Uno.UI.Hosting;

namespace UnoApp50.Skia.Gtk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppHead.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new AppHead())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();
    }
}
