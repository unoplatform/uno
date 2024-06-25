using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace uno54wasmskia;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = new PlatformHost(() => new App());
        await host.Run();
    }
}
