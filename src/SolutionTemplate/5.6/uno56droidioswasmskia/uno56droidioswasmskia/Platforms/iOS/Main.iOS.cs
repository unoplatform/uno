using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace uno56droidioswasmskia.iOS;

public class EntryPoint
{
    // This is the main entry point of the application.
    public static void Main(string[] args)
    {
		var host = new AppleUIKitHost(() => new App());
		host.Run();
    }
}
