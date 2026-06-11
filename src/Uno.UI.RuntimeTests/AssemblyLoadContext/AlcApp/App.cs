using Microsoft.Extensions.Logging;
using System.Linq;

namespace AlcTestApp;

public partial class App : Application
{
	internal static Window? TestWindow;
	internal static string[] LaunchArguments { get; set; } = Array.Empty<string>();
	private const string UseFrameArgument = "--use-frame";
	private const string DeferContentArgument = "--defer-content";
	private static bool _useFrameContent;
	private static bool _deferContent;

	public App()
	{
		this.InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		TestWindow = new Window();

		_useFrameContent = LaunchArguments.Any(arg => string.Equals(arg, UseFrameArgument, StringComparison.OrdinalIgnoreCase));
		_deferContent = LaunchArguments.Any(arg => string.Equals(arg, DeferContentArgument, StringComparison.OrdinalIgnoreCase));

		if (!_deferContent)
		{
			ApplyContent(TestWindow);
			// Ensure the current window is active
			TestWindow.Activate();
		}
	}

	internal static void ApplyDeferredContent()
	{
		if (!_deferContent || TestWindow is null)
		{
			return;
		}

		_ = TestWindow.DispatcherQueue.TryEnqueue(() => ApplyContent(TestWindow));
	}

	private static void ApplyContent(Window window)
	{
		if (_useFrameContent)
		{
			var frame = new Frame();
			window.Content = frame;
			frame.Navigate(typeof(MainPage));
		}
		else
		{
			// For ALC testing, we set content to a simple page
			window.Content = new MainPage();
		}
	}
}
