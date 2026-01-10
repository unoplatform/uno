using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

#if !WINDOWS_UWP && !WINAPPSDK
using Uno.UI.Xaml.Controls;
#endif

namespace UITests.Windows_UI_Xaml.WindowTests;

[Sample(
	"Window",
	IsManualTest = true,
	Description =
		"This sample demonstrates the Application.DispatcherShutdownMode property. " +
		"With OnLastWindowClose (default), the app exits when all windows close. " +
		"With OnExplicitShutdown, the app continues running even after all windows are closed.")]
public sealed partial class Window_DispatcherShutdownMode : Page
{
	private int _windowCounter = 0;

	public Window_DispatcherShutdownMode()
	{
		this.InitializeComponent();
		UpdateCurrentMode();
		CheckMultipleWindowsSupport();
	}

	private void CheckMultipleWindowsSupport()
	{
#if HAS_UNO
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			LogMessage("This platform does not support multiple windows.");
			OpenWindowButton.IsEnabled = false;
			OnLastWindowCloseButton.IsEnabled = false;
			OnExplicitShutdownButton.IsEnabled = false;
		}
#endif
	}

	private void UpdateCurrentMode()
	{
		var mode = Application.Current.DispatcherShutdownMode;
		CurrentModeTextBlock.Text = mode.ToString();
		LogMessage($"Current DispatcherShutdownMode: {mode}");
	}

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

	private void OnOpenWindow(object sender, RoutedEventArgs e)
	{
		_windowCounter++;
		var windowNumber = _windowCounter;
		
		var secondaryWindow = new Window();
		secondaryWindow.Title = $"Secondary Window {windowNumber}";
		
		var content = new StackPanel
		{
			Padding = new Thickness(16),
			Spacing = 12,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};

		content.Children.Add(new TextBlock
		{
			Text = $"Secondary Window {windowNumber}",
			FontSize = 20,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Center
		});

		content.Children.Add(new TextBlock
		{
			Text = "Close this window to test the shutdown behavior.",
			TextWrapping = TextWrapping.Wrap,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 12)
		});

		content.Children.Add(new Button
		{
			Content = "Close Window",
			HorizontalAlignment = HorizontalAlignment.Center,
			Command = new DelegateCommand(() => secondaryWindow.Close())
		});

		secondaryWindow.Content = content;
		
		secondaryWindow.Closed += (s, args) =>
		{
			LogMessage($"Window {windowNumber} closed");
		};

		secondaryWindow.Activate();
		LogMessage($"Opened Window {windowNumber}");
	}

	private void OnExitApplication(object sender, RoutedEventArgs e)
	{
		LogMessage("Calling Application.Current.Exit()");
		Application.Current.Exit();
	}

	private void LogMessage(string message)
	{
		var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
		var logMessage = $"[{timestamp}] {message}\n";
		LogTextBlock.Text += logMessage;
	}
}

// Simple command implementation for the button
internal class DelegateCommand : System.Windows.Input.ICommand
{
	private readonly Action _execute;

	public DelegateCommand(Action execute)
	{
		_execute = execute;
	}

	public event EventHandler CanExecuteChanged;

	public bool CanExecute(object parameter) => true;

	public void Execute(object parameter) => _execute?.Invoke();
}
