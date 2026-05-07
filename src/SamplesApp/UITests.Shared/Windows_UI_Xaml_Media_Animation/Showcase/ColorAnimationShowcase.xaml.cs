using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Comprehensive showcase of ColorAnimation: Background, Fill, Stroke, Foreground, EasingFunction and looping color stripes.")]
public sealed partial class ColorAnimationShowcase : Page
{
	public ColorAnimationShowcase()
	{
		this.InitializeComponent();
	}

	private static void Run(SolidColorBrush target, Color from, Color to, TimeSpan duration, EasingFunctionBase easing = null)
	{
		var animation = new ColorAnimation
		{
			From = from,
			To = to,
			Duration = new Duration(duration),
			EasingFunction = easing,
			EnableDependentAnimation = true,
		};
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, "Color");
		new Storyboard { Children = { animation } }.Begin();
	}

	private void RunBackground(object sender, RoutedEventArgs e)
	{
		Run(BackgroundBrush, Color.FromArgb(0xFF, 0x3F, 0x51, 0xB5), Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50), TimeSpan.FromSeconds(1.2));
	}

	private void RunFill(object sender, RoutedEventArgs e)
	{
		Run(FillBrush, Color.FromArgb(0xFF, 0xE9, 0x1E, 0x63), Color.FromArgb(0xFF, 0xFF, 0xC1, 0x07), TimeSpan.FromSeconds(1.2));
	}

	private void RunStroke(object sender, RoutedEventArgs e)
	{
		Run(StrokeBrush, Color.FromArgb(0xFF, 0x00, 0x96, 0x88), Color.FromArgb(0xFF, 0xF4, 0x43, 0x36), TimeSpan.FromSeconds(1.2));
	}

	private void RunForeground(object sender, RoutedEventArgs e)
	{
		Run(ForegroundBrush, Color.FromArgb(0xFF, 0xFF, 0x57, 0x22), Color.FromArgb(0xFF, 0x21, 0x96, 0xF3), TimeSpan.FromSeconds(1.2));
	}

	private void RunEasing(object sender, RoutedEventArgs e)
	{
		var blue = Color.FromArgb(0xFF, 0x19, 0x76, 0xD2);
		var red = Color.FromArgb(0xFF, 0xD3, 0x2F, 0x2F);

		Run(EaseLinearBrush, blue, red, TimeSpan.FromSeconds(2));
		Run(EaseCurvedBrush, blue, red, TimeSpan.FromSeconds(2), new CubicEase { EasingMode = EasingMode.EaseInOut });
	}

	private void StartStripes(object sender, RoutedEventArgs e)
	{
		StripeStoryboard.Begin();
	}

	private void StopStripes(object sender, RoutedEventArgs e)
	{
		StripeStoryboard.Stop();
	}
}
