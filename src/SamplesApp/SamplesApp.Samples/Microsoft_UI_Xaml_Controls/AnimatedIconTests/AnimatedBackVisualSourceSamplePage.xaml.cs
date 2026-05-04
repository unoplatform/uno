using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Windows.UI;

namespace UITests.Microsoft_UI_Xaml_Controls.AnimatedIconTests;

[Sample("Icons", IsManualTest = true)]
public sealed partial class AnimatedBackVisualSourceSamplePage : Page
{
	public AnimatedBackVisualSourceSamplePage()
	{
		InitializeComponent();
	}

	// Use the IAnimatedVisualSource2.Markers dictionary instead of the source's
	// Uno-only marker constants so the sample stays portable to native WinUI.
	private (double from, double to) Marker(string transition)
	{
		var markers = BackSource.Markers;
		return (markers[transition + "_Start"], markers[transition + "_End"]);
	}

	private async void OnBackClicked(object sender, RoutedEventArgs e)
	{
		// Simulate a press animation by playing across the press transition markers.
		StatusText.Text = "Click: playing Normal → Pressed → Normal";
		var (from1, to1) = Marker("NormalToPressed");
		await Player.PlayAsync(from1, to1, false);
		var (from2, to2) = Marker("PressedToNormal");
		await Player.PlayAsync(from2, to2, false);
		StatusText.Text = "Click: done";
	}

	private async void OnNormalToPointerOver(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing Normal → PointerOver";
		var (from, to) = Marker("NormalToPointerOver");
		await Player.PlayAsync(from, to, false);
		StatusText.Text = "Done: Normal → PointerOver";
	}

	private async void OnPointerOverToNormal(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing PointerOver → Normal";
		var (from, to) = Marker("PointerOverToNormal");
		await Player.PlayAsync(from, to, false);
		StatusText.Text = "Done: PointerOver → Normal";
	}

	private async void OnNormalToPressed(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing Normal → Pressed";
		var (from, to) = Marker("NormalToPressed");
		await Player.PlayAsync(from, to, false);
		StatusText.Text = "Done: Normal → Pressed";
	}

	private async void OnPressedToNormal(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing Pressed → Normal";
		var (from, to) = Marker("PressedToNormal");
		await Player.PlayAsync(from, to, false);
		StatusText.Text = "Done: Pressed → Normal";
	}

	private void OnProgressSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
	{
		Player.SetProgress(e.NewValue);
		StatusText.Text = $"Progress: {e.NewValue:F2}";
	}

	private void OnBlackForeground(object sender, RoutedEventArgs e)
		=> BackSource.SetColorProperty("Foreground", Color.FromArgb(0xFF, 0x00, 0x00, 0x00));

	private void OnRedForeground(object sender, RoutedEventArgs e)
		=> BackSource.SetColorProperty("Foreground", Color.FromArgb(0xFF, 0xE8, 0x10, 0x23));

	private void OnWhiteForeground(object sender, RoutedEventArgs e)
		=> BackSource.SetColorProperty("Foreground", Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
}
