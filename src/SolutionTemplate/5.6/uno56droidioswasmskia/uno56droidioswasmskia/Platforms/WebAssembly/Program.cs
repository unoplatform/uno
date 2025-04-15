using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace uno56droidioswasmskia;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = new WebAssemblyBrowserHost(() => new App());
        await host.Run();
    }
}
