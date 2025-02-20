using Uno.UI.Runtime.Skia;

namespace uno56net9win32;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseWin32()
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .Build();

        host.Run();
    }
}
