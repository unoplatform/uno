#nullable enable

using System;
using Microsoft.UI.Windowing;
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
		"Each window the user can see raises AppWindow.Closing, front to back, then Window.Closed; a cancelled Closing cancels the quit and leaves the remaining windows open. " +
		"Once they are closed the app exits, whatever the shutdown mode: OnExplicitShutdown only keeps it running after its last window is closed, and a later Quit still ends it. " +
		"Quit is refused while a file picker or a sheet is open. A logout, restart or shutdown ends the app without AppWindow.Closing or Window.Closed. " +
		"Application.Current.Exit() closes every window as WinUI does, raising Window.Closed but not AppWindow.Closing, then exits. " +
		"Run the app from a terminal: the events are also written to the console, which outlives the windows.")]
public sealed partial class AppWindow_QuitClosesWindows : Page
{
	private static AppWindow_QuitClosesWindows? _current;
	private static int _secondaryWindowCount;

	private readonly DispatcherShutdownMode _initialShutdownMode = Application.Current.DispatcherShutdownMode;
	private Window? _mainWindow;
	private bool _mainWindowClosed;

	public AppWindow_QuitClosesWindows()
	{
		this.InitializeComponent();

		_current = this;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;

		if (!SupportsMultipleWindows)
		{
			OpenSecondaryWindowButton.IsEnabled = false;
			OpenCancellingSecondaryWindowButton.IsEnabled = false;
		}

		UpdateCurrentMode();
	}

	private static bool SupportsMultipleWindows =>
#if HAS_UNO
		NativeWindowFactory.SupportsMultipleWindows;
#else
		true;
#endif

	// Hooked while the page is shown, so leaving it leaves the main window as it was; Closed is raised before Unloaded.
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_current = this;
		if (_mainWindow is null && App.MainWindow is { } main)
		{
			_mainWindow = main;
			main.AppWindow.Closing += OnMainWindowClosing;
			main.Closed += OnMainWindowClosed;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_current == this)
		{
			_current = null;
		}

		if (_mainWindow is { } main)
		{
			main.AppWindow.Closing -= OnMainWindowClosing;
			main.Closed -= OnMainWindowClosed;
			_mainWindow = null;
		}

		// Leaving the sample restores the mode; closing its window must not, or OnExplicitShutdown would end the app.
		if (!_mainWindowClosed)
		{
			Application.Current.DispatcherShutdownMode = _initialShutdownMode;
		}
	}

	private void OnMainWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
	{
		args.Cancel = CancelMainClosingCheckBox.IsChecked == true;
		Log($"main window: AppWindow.Closing (Cancel = {args.Cancel})");
	}

	private void OnMainWindowClosed(object sender, WindowEventArgs args)
	{
		_mainWindowClosed = true;
		Log("main window: Window.Closed");
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
