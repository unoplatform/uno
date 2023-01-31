using AppKit;

namespace $ext_safeprojectname$
{
	// This is the main entry point of the application.
	public class EntryPoint
	{
		static void Main(string[] args)
		{
			NSApplication.Init();
<<<<<<< HEAD
			NSApplication.SharedApplication.Delegate = new App();
			NSApplication.Main(args);  
=======
			NSApplication.SharedApplication.Delegate = new AppHead();
			NSApplication.Main(args);
>>>>>>> a27ca7001c (chore: Adjust AppHead references)
		}
	}
}

