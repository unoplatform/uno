#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SamplesApp;
using Uno.UI.Samples.Controls;

#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

namespace UITests.Microsoft_UI_Windowing;

[Sample(
	"Windowing",
	IsManualTest = true,
	IgnoreInSnapshotTests = true,
	Description =
		"macOS: quit the app with Cmd+Q, the application menu's Quit or the Dock's Quit. " +
		"Each window raises AppWindow.Closing, front to back, then Window.Closed; a cancelled Closing cancels the quit and leaves the remaining windows open. " +
		"Once every window is closed, OnLastWindowClose exits and OnExplicitShutdown keeps the process running until Application.Current.Exit(). " +
		"Application.Current.Exit() exits without raising AppWindow.Closing. " +
		"Run the app from a terminal: the events are also written to the console, which outlives the windows.")]
public sealed partial class AppWindow_QuitClosesWindows : Page
{
	private static AppWindow_QuitClosesWindows? _current;
	private static bool _cancelMainClosing;
	private static bool _mainWindowHooked;
	private static int _secondaryWindowCount;

	public AppWindow_QuitClosesWindows()
	{
		this.InitializeComponent();

		_current = this;
		Loaded += (_, _) => _current = this;
		Unloaded += (_, _) => _current = _current == this ? null : _current;

		CancelMainClosingCheckBox.IsChecked = _cancelMainClosing;
		if (!SupportsMultipleWindows)
		{
			OpenSecondaryWindowButton.IsEnabled = false;
			OpenCancellingSecondaryWindowButton.IsEnabled = false;
		}

		HookMainWindow();
		UpdateCurrentMode();
	}

	private static bool SupportsMultipleWindows =>
#if HAS_UNO
		NativeWindowFactory.SupportsMultipleWindows;
#else
		true;
#endif

	// Once per process: the main window outlives this page, and its Closed is raised after the page may be unloaded.
	private static void HookMainWindow()
	{
		if (_mainWindowHooked || App.MainWindow is not { } main)
		{
			return;
		}

		_mainWindowHooked = true;
		main.AppWindow.Closing += (_, args) =>
		{
			args.Cancel = _cancelMainClosing;
			Log($"main window: AppWindow.Closing (Cancel = {args.Cancel})");
		};
		main.Closed += (_, _) => Log("main window: Window.Closed");
	}

	private void OnCancelMainClosingChanged(object sender, RoutedEventArgs e)
		=> _cancelMainClosing = CancelMainClosingCheckBox.IsChecked == true;

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

	private void OnOpenSecondaryWindow(object sender, RoutedEventArgs e) => OpenSecondaryWindow(cancelClosing: false);

	private void OnOpenCancellingSecondaryWindow(object sender, RoutedEventArgs e) => OpenSecondaryWindow(cancelClosing: true);

	private static void OpenSecondaryWindow(bool cancelClosing)
	{
		var name = $"secondary window {++_secondaryWindowCount}";
		var window = new Window { Title = name };
		var cancel = new CheckBox { Content = "Cancel AppWindow.Closing", IsChecked = cancelClosing };
		window.Content = new StackPanel
		{
			Padding = new Thickness(16),
			Spacing = 8,
			Children = { new TextBlock { Text = name }, cancel },
		};
		window.AppWindow.Closing += (_, args) =>
		{
			args.Cancel = cancel.IsChecked == true;
			Log($"{name}: AppWindow.Closing (Cancel = {args.Cancel})");
		};
		window.Closed += (_, _) => Log($"{name}: Window.Closed");
		window.Activate();
		Log($"{name}: opened");
	}

	private void OnExit(object sender, RoutedEventArgs e)
	{
		Log("Application.Current.Exit()");
		Application.Current.Exit();
	}

	private void UpdateCurrentMode()
	{
		CurrentModeText.Text = Application.Current.DispatcherShutdownMode.ToString();
		Log($"shutdown mode: {Application.Current.DispatcherShutdownMode}");
	}

	private static void Log(string message)
	{
		var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
		Console.WriteLine($"[{nameof(AppWindow_QuitClosesWindows)}] {line}");
		if (_current is { } page)
		{
			page.LogTextBlock.Text = line + Environment.NewLine + page.LogTextBlock.Text;
		}
	}
}
