using System.Threading.Tasks;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace SamplesApp.iOS
{
	public class Application
	{
		// This is the main entry point of the application.
		static async Task Main(string[] args)
		{
			await (new AppleUIKitSkiaHost(() => new SamplesApp.App(), args)).Run();
		}
	}
}
