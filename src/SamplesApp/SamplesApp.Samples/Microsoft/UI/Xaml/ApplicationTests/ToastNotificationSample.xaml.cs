using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Extensibility;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.ApplicationTests;

[Sample("Application", Name = "Toast Notification Sample", IsManualTest = true, Description = "Android-only. Clicking the button displays a toast notification. Tapping this notification should just activate the app, not make the screen blank.")]
public sealed partial class ToastNotificationSample : Page
{
	private readonly IToastNotificationSampleProvider _toastNotificationSampleProvider;

	public ToastNotificationSample()
	{
		this.InitializeComponent();

		ApiExtensibility.CreateInstance(this, out _toastNotificationSampleProvider);
	}

	private async void ShowToastButton_Click(object sender, RoutedEventArgs e)
	{
		var title = TitleTextBox.Text?.Trim();
		var content = ContentTextBox.Text?.Trim();

		if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
		{
			StatusTextBlock.Text = "Please enter both title and content";
			return;
		}

		StatusTextBlock.Text = "Showing notification...";

		try
		{
			await Toast(title, content);
			StatusTextBlock.Text = "Notification shown successfully!";
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = $"Error: {ex.Message}";
		}
	}

	private async Task Toast(string p_title, string p_content)
	{
		if (string.IsNullOrEmpty(p_title) || string.IsNullOrEmpty(p_content))
			return;

		if (_toastNotificationSampleProvider is { } provider)
		{
			await provider.ShowToastAsync(p_title, p_content);
		}
	}
}
