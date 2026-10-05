using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation;

[Sample(
	"Automation",
	Name = "Accessibility_Showcase",
	Description = "One page per screen-reader capability: labels, descriptions, headings, toggles, ranges, lists, expanders, form validation, live regions, notifications, dialogs, landmarks and hidden content. Each section states what should be announced.",
	IsManualTest = true)]
public sealed partial class Accessibility_Showcase : Page
{
	private readonly DispatcherTimer _downloadTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
	private int _downloadProgress;
	private int _clickCount;
	private int _disconnectCount;

	public Accessibility_Showcase()
	{
		this.InitializeComponent();

		_downloadTimer.Tick += OnDownloadTick;
		Unloaded += (_, _) => _downloadTimer.Stop();
	}

	private void OnCountClicked(object sender, RoutedEventArgs e)
	{
		_clickCount++;
		Notify((Button)sender, $"Clicked {_clickCount} {(_clickCount == 1 ? "time" : "times")}", "ClickCount");
	}

	private void OnSubmitClicked(object sender, RoutedEventArgs e)
	{
		// FormError is an assertive live region: setting its text is all it takes to be announced.
		if (string.IsNullOrWhiteSpace(EmailBox.Text))
		{
			FormError.Text = "Error: Email is required.";
		}
		else if (!EmailBox.Text.Contains('@'))
		{
			FormError.Text = "Error: Email must contain an @ sign.";
		}
		else if (string.IsNullOrEmpty(PasswordBox.Password))
		{
			FormError.Text = "Error: Password is required.";
		}
		else
		{
			FormError.Text = string.Empty;
			Notify((Button)sender, "Form submitted successfully", "FormSubmitted");
		}
	}

	private void OnStartDownloadClicked(object sender, RoutedEventArgs e)
	{
		_downloadProgress = 0;
		DownloadStatus.Text = "Download started";
		_downloadTimer.Start();
	}

	private void OnDownloadTick(object sender, object e)
	{
		_downloadProgress += 25;
		if (_downloadProgress >= 100)
		{
			_downloadTimer.Stop();
			DownloadStatus.Text = "Download complete";
		}
		else
		{
			DownloadStatus.Text = $"Downloading, {_downloadProgress} percent";
		}
	}

	private void OnNotifyClicked(object sender, RoutedEventArgs e)
		=> Notify(NotifyButton, "Report saved", "ReportSaved");

	private void OnDisconnectClicked(object sender, RoutedEventArgs e)
	{
		// The text must change for the live region to fire again on a repeated click.
		_disconnectCount++;
		ConnectionStatus.Text = $"Connection lost (attempt {_disconnectCount}). Retrying in 5 seconds.";
	}

	private async void OnOpenDialogClicked(object sender, RoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			Title = "Delete file?",
			Content = "report.pdf will be permanently deleted.",
			PrimaryButtonText = "Delete",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot,
		};

		var result = await dialog.ShowAsync();
		DialogResult.Text = result == ContentDialogResult.Primary ? "report.pdf deleted" : "Deletion cancelled";
	}

	// Notifications are announced without a visible live region, like a toast for screen readers.
	private static void Notify(UIElement source, string message, string activityId)
	{
		var peer = FrameworkElementAutomationPeer.FromElement(source) ?? FrameworkElementAutomationPeer.CreatePeerForElement(source);
		peer?.RaiseNotificationEvent(
			AutomationNotificationKind.ActionCompleted,
			AutomationNotificationProcessing.ImportantMostRecent,
			message,
			activityId);
	}
}
