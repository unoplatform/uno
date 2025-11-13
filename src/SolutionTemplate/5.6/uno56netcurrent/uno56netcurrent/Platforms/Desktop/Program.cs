using Uno.UI.Hosting;

namespace uno56netcurrent;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var exit = args.Any(a => a == "--exit");

        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App(exit))
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
