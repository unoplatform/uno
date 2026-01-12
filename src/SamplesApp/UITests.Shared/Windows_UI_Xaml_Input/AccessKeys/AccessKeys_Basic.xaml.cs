using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Input.AccessKeys;

[Sample("AccessKeys", IsManualTest = true,
	Description = "Press Alt to enter access key mode. Type the access key (A, B, C, 1, 2, 3) to invoke buttons. Press Escape to exit.")]
public sealed partial class AccessKeys_Basic : Page
{
	public AccessKeys_Basic()
	{
		this.InitializeComponent();

		// Subscribe to access key events
		SetupAccessKeyEvents(ButtonA);
		SetupAccessKeyEvents(ButtonB);
		SetupAccessKeyEvents(ButtonC);
		SetupAccessKeyEvents(Button1);
		SetupAccessKeyEvents(Button2);
		SetupAccessKeyEvents(Button3);
		SetupAccessKeyEvents(ScopeButton);
		SetupAccessKeyEvents(NestedX);
		SetupAccessKeyEvents(NestedY);
		SetupAccessKeyEvents(NestedZ);

		// Subscribe to AccessKeyManager events
		AccessKeyManager.IsDisplayModeEnabledChanged += OnAccessKeyModeChanged;
	}

	private void SetupAccessKeyEvents(UIElement element)
	{
		element.AccessKeyDisplayRequested += (s, e) =>
		{
			var name = (s as FrameworkElement)?.Name ?? "Unknown";
			LogEvent($"[{name}] AccessKeyDisplayRequested - PressedKeys: '{e.PressedKeys}'");
		};

		element.AccessKeyDisplayDismissed += (s, e) =>
		{
			var name = (s as FrameworkElement)?.Name ?? "Unknown";
			LogEvent($"[{name}] AccessKeyDisplayDismissed");
		};

		element.AccessKeyInvoked += (s, e) =>
		{
			var name = (s as FrameworkElement)?.Name ?? "Unknown";
			LogEvent($"[{name}] AccessKeyInvoked - Handled: {e.Handled}");
		};
	}

	private void OnAccessKeyModeChanged(object sender, object e)
	{
		var isActive = AccessKeyManager.IsDisplayModeEnabled;
		ModeStatus.Text = $"Access Key Mode: {(isActive ? "Active" : "Inactive")}";
		LogEvent($"AccessKeyManager.IsDisplayModeEnabledChanged - IsActive: {isActive}");
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;
		var name = button?.Name ?? "Unknown";
		LastAction.Text = $"Last Action: {name} clicked";
		LogEvent($"[{name}] Button.Click");
	}

	private void ClearLog_Click(object sender, RoutedEventArgs e)
	{
		EventLog.Text = string.Empty;
	}

	private void LogEvent(string message)
	{
		var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
		EventLog.Text = $"[{timestamp}] {message}\n{EventLog.Text}";
	}
}
