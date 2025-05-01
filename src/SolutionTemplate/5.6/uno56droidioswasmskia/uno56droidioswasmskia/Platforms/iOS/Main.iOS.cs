using UIKit;
using Uno.UI.Hosting;

namespace uno56droidioswasmskia.iOS;

public class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new SamplesApp.App())
            .UseAppleUIKit()
            .Build(); 
        
        host.Run();
    }
}
