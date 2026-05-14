using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement", Name = "SoftInput_InContentDialog", Description = "TextBox inside a ContentDialog. Dialog should adjust above keyboard.")]
public sealed partial class SoftInputTests_InContentDialog : Page
{
	public SoftInputTests_InContentDialog()
	{
		this.InitializeComponent();
		this.Loaded += (_, _) =>
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing += (s, e) => UpdateStatus(s.OccludedRect, "Showing");
			inputPane.Hiding += (s, e) => UpdateStatus(s.OccludedRect, "Hiding");
		};
	}

	private async void OnOpenDialog(object sender, RoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			Title = "Enter Information",
			Content = new StackPanel
			{
				Spacing = 8,
				Children =
				{
					new TextBlock { Text = "This dialog should move above the keyboard:" },
					new TextBox { PlaceholderText = "Tap here to open keyboard..." },
					new TextBox { PlaceholderText = "Second field (test focus change)..." },
				}
			},
			PrimaryButtonText = "OK",
			CloseButtonText = "Cancel",
			XamlRoot = this.XamlRoot,
		};

		await dialog.ShowAsync();
	}

	private void UpdateStatus(Windows.Foundation.Rect rect, string evt) =>
		StatusText.Text = $"InputPane: {evt} | OccludedRect: {rect}";
}
