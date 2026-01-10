using Microsoft.Extensions.Logging;

namespace AlcTestApp;

public partial class App : Application
{
	internal static Window? TestWindow;

	public App()
	{
		this.InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		TestWindow = new Window();

		// For ALC testing, we set content to a simple page
		TestWindow.Content = new MainPage();

		// Ensure the current window is active
		TestWindow.Activate();
	}
}
