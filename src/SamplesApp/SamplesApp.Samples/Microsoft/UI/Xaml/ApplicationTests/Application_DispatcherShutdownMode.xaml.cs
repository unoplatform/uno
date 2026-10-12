using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

#if HAS_UNO
using Uno.UI;
using Uno.UI.Xaml.Controls;
#endif

namespace UITests.Windows_UI_Xaml.ApplicationTests;

[Sample(
	"Application",
	IsManualTest = true,
	Description =
		"Demonstrates Application.DispatcherShutdownMode. " +
		"Default is OnLastWindowClose: closing every window exits the app. " +
		"Switch to OnExplicitShutdown to keep the process alive after all windows are closed " +
		"(use Application.Current.Exit() to terminate).")]
public sealed partial class Application_DispatcherShutdownMode : Page
{
	public Application_DispatcherShutdownMode()
	{
		this.InitializeComponent();

		if (!SupportsMultipleWindows)
		{
			OpenSecondaryWindowButton.IsEnabled = false;
			Log("This platform does not support multiple windows.");
		}

		UpdateCurrentMode();
	}

	private static bool SupportsMultipleWindows =>
#if HAS_UNO
		NativeWindowFactory.SupportsMultipleWindows;
#else
		true;
#endif

	private void OnSetOnLastWindowClose(object sender, RoutedEventArgs e)
	{
		Application.Current.DispatcherShutdownMode = DispatcherShutdownMode.OnLastWindowClose;
		UpdateCurrentMode();
	}

	private void OnSetOnExplicitShutdown(object sender, RoutedEventArgs e)
	{
		Application.Current.DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
		UpdateCurrentMode();
	}

	private void OnOpenSecondaryWindow(object sender, RoutedEventArgs e)
	{
		var window = new Window();
		var closeButton = new Button { Content = "Close this window" };
		closeButton.Click += (_, _) => window.Close();
		window.Content = new StackPanel
		{
			Padding = new Thickness(16),
			Spacing = 8,
			Children =
			{
				new TextBlock { Text = "Secondary window" },
				closeButton,
			},
		};
		window.Activate();
		Log($"Opened a secondary window.");
	}

	private void OnCloseAllWindows(object sender, RoutedEventArgs e)
	{
#if HAS_UNO
		var windows = ApplicationHelper.Windows.ToArray();
		foreach (var window in windows)
		{
			window.Close();
		}
		Log($"Closed {windows.Length} window(s). With OnExplicitShutdown, the process should remain alive.");
#else
		Log("Closing windows is not implemented in this sample on this platform.");
#endif
	}

	private void OnExit(object sender, RoutedEventArgs e)
	{
		Application.Current.Exit();
	}

	private void UpdateCurrentMode()
	{
		CurrentModeText.Text = Application.Current.DispatcherShutdownMode.ToString();
	}

	private void Log(string message)
	{
		LogTextBlock.Text = message;
	}
}
