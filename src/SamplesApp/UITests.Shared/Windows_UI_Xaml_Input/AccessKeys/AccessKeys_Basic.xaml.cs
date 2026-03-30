using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Input.AccessKeys;

[Sample("AccessKey", Description = "Demonstrates AccessKey (Alt+key) navigation and invocation",
	IsManualTest = true, IgnoreInSnapshotTests = true)]
public sealed partial class AccessKeys_Basic : Page
{
	private int _logCounter;

	public AccessKeys_Basic()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		// Subscribe to AccessKeyManager display mode changes
		AccessKeyManager.IsDisplayModeEnabledChanged += OnDisplayModeChanged;

		// Set up AccessKeyInvoked handlers for all test buttons
		SetupAccessKeyHandlers(ButtonA);
		SetupAccessKeyHandlers(ButtonB);
		SetupAccessKeyHandlers(ButtonC);
		SetupAccessKeyHandlers(ButtonXY);
		SetupAccessKeyHandlers(ButtonXZ);
		SetupAccessKeyHandlers(CheckD);
		SetupAccessKeyHandlers(ToggleE);
		SetupAccessKeyHandlers(ScopeButton1);
		SetupAccessKeyHandlers(ScopeButton2);
		SetupAccessKeyHandlers(ButtonStay);
		SetupAccessKeyHandlers(ButtonDisabled);
		SetupAccessKeyHandlers(ButtonH);

		Log("Page loaded. Press Alt to enter access key mode.");
	}

	private void SetupAccessKeyHandlers(UIElement element)
	{
		var name = (element as FrameworkElement)?.Name ?? element.GetType().Name;

		element.AccessKeyInvoked += (sender, args) =>
		{
			Log($"[AccessKeyInvoked] {name} (Handled={args.Handled})");
		};

#if __SKIA__ || WINAPPSDK
		element.AccessKeyDisplayRequested += (sender, args) =>
		{
			Log($"[AccessKeyDisplayRequested] {name} (PressedKeys=\"{args.PressedKeys}\")");
		};

		element.AccessKeyDisplayDismissed += (sender, args) =>
		{
			Log($"[AccessKeyDisplayDismissed] {name}");
		};
#endif
	}

	private void OnDisplayModeChanged(object sender, object args)
	{
		var isActive = AccessKeyManager.IsDisplayModeEnabled;
		DisplayModeIndicator.Text = isActive ? "Yes" : "No";
		Log($"[IsDisplayModeEnabledChanged] IsDisplayModeEnabled={isActive}");
	}

	private void EnterModeButton_Click(object sender, RoutedEventArgs e)
	{
		Log("[Programmatic] Entering display mode...");
		AccessKeyManager.EnterDisplayMode(this.XamlRoot);
	}

	private void ExitModeButton_Click(object sender, RoutedEventArgs e)
	{
		Log("[Programmatic] Exiting display mode...");
		AccessKeyManager.ExitDisplayMode();
	}

	private void ClearLogButton_Click(object sender, RoutedEventArgs e)
	{
		_logCounter = 0;
		EventLog.Text = string.Empty;
	}

	private void Log(string message)
	{
		_logCounter++;
		EventLog.Text += $"{_logCounter}. {message}\n";

		// Auto-scroll to bottom
		LogScroller?.ChangeView(null, LogScroller.ScrollableHeight, null);
	}
}
