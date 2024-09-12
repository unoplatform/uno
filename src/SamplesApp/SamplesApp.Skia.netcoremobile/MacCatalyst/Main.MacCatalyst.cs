using System.Threading.Tasks;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace SamplesApp.MacCatalyst;

public class Application
{
	// This is the main entry point of the application.
	public static async Task Main(string[] args)
	{
		var host = new PlatformHost(() => new SamplesApp.App());
		await host.Run();
	}
}
