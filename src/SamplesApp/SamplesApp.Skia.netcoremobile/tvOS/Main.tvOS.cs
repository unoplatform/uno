using System.Threading.Tasks;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace SamplesApp.tvOS;

public class Application
{
	public static void Main(string[] args)
	{
		var host = new PlatformHost(() => new SamplesApp.App());
		host.Run();
	}
}
