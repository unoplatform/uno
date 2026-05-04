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

	private async void OnBackClicked(object sender, RoutedEventArgs e)
	{
		// Simulate a press animation by playing across the press transition markers.
		StatusText.Text = "Click: playing Normal → Pressed → Normal";
		await Player.PlayAsync(AnimatedBackVisualSource.M_NormalToPressed_Start, AnimatedBackVisualSource.M_NormalToPressed_End, false);
		await Player.PlayAsync(AnimatedBackVisualSource.M_PressedToNormal_Start, AnimatedBackVisualSource.M_PressedToNormal_End, false);
		StatusText.Text = "Click: done";
	}

	private async void OnNormalToPointerOver(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing Normal → PointerOver";
		await Player.PlayAsync(AnimatedBackVisualSource.M_NormalToPointerOver_Start, AnimatedBackVisualSource.M_NormalToPointerOver_End, false);
		StatusText.Text = "Done: Normal → PointerOver";
	}

	private async void OnPointerOverToNormal(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing PointerOver → Normal";
		await Player.PlayAsync(AnimatedBackVisualSource.M_PointerOverToNormal_Start, AnimatedBackVisualSource.M_PointerOverToNormal_End, false);
		StatusText.Text = "Done: PointerOver → Normal";
	}

	private async void OnNormalToPressed(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing Normal → Pressed";
		await Player.PlayAsync(AnimatedBackVisualSource.M_NormalToPressed_Start, AnimatedBackVisualSource.M_NormalToPressed_End, false);
		StatusText.Text = "Done: Normal → Pressed";
	}

	private async void OnPressedToNormal(object sender, RoutedEventArgs e)
	{
		StatusText.Text = "Playing Pressed → Normal";
		await Player.PlayAsync(AnimatedBackVisualSource.M_PressedToNormal_Start, AnimatedBackVisualSource.M_PressedToNormal_End, false);
		StatusText.Text = "Done: Pressed → Normal";
	}

	private void OnProgressSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
	{
		Player.SetProgress(e.NewValue);
		StatusText.Text = $"Progress: {e.NewValue:F2}";
	}

	private void OnBlackForeground(object sender, RoutedEventArgs e)
	{
		BackSource.Foreground = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
	}

	private void OnRedForeground(object sender, RoutedEventArgs e)
	{
		BackSource.Foreground = Color.FromArgb(0xFF, 0xE8, 0x10, 0x23);
	}

	private void OnWhiteForeground(object sender, RoutedEventArgs e)
	{
		BackSource.Foreground = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
	}
}
