using Uno.UI.Hosting;
namespace uno56droidioswasmskia;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer(builder => builder.DisplayScale(2.0f)) // Use .UseLinuxFrameBuffer() for default behavior
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}