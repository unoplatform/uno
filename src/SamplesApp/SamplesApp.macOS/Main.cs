using AppKit;

namespace SamplesApp.macOS
{
	internal static class MainClass
	{
		private static void Main(string[] args)
		{
			NSApplication.Init();
			NSApplication.SharedApplication.Delegate = new App();
			NSApplication.Main(args);  
		}
	}
}

