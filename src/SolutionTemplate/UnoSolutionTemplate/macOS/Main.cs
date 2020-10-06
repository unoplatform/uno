using AppKit;

namespace $ext_safeprojectname$
{
	public partial class App
	{
		static void Main(string[] args)
		{
			NSApplication.Init();
			NSApplication.SharedApplication.Delegate = new App();
			NSApplication.Main(args);
		}
	}
}

