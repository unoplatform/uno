using UIKit;

namespace SamplesApp.iOS;

public class Application
{
	// This is the main entry point of the application.
	static void Main(string[] args)
	{
#if __IOS__ && !__MACCATALYST__ && !TESTFLIGHT && !DEBUG
		// requires Xamarin Test Cloud Agent
		Xamarin.Calabash.Start();
#endif

		// if you want to use a different Application Delegate class from "AppDelegate"
		// you can specify it here.
		UIApplication.Main(args, null, typeof(App));
	}
}
