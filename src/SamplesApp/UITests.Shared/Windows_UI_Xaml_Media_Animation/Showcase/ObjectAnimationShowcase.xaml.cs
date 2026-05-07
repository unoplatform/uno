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
	Description = "Comprehensive showcase of ObjectAnimationUsingKeyFrames: Visibility blinks, brush swaps, text swaps and code-driven discrete value changes.")]
public sealed partial class ObjectAnimationShowcase : Page
{
	// Segoe MDL2 Assets glyphs (private use area).
	private const string GlyphWifi = "";
	private const string GlyphChevronRight = "";
	private const string GlyphCheckmark = "";

	public ObjectAnimationShowcase()
	{
		this.InitializeComponent();
	}

	private void StartCycle(object sender, RoutedEventArgs e)
	{
		CycleStoryboard.Begin();
	}

	private void StopCycle(object sender, RoutedEventArgs e)
	{
		CycleStoryboard.Stop();
	}

	private void RunCodeSwap(object sender, RoutedEventArgs e)
	{
		CodeTarget.CornerRadius = new CornerRadius(0);
		CodeTarget.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x77, 0x88, 0x99));
		CodeIcon.Glyph = GlyphWifi;

		var radius = new ObjectAnimationUsingKeyFrames();
		radius.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = new CornerRadius(0) });
		radius.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.FromSeconds(0.6), Value = new CornerRadius(20) });
		radius.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.FromSeconds(1.2), Value = new CornerRadius(60) });
		Storyboard.SetTarget(radius, CodeTarget);
		Storyboard.SetTargetProperty(radius, "CornerRadius");

		var background = new ObjectAnimationUsingKeyFrames();
		background.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = TimeSpan.Zero,
			Value = new SolidColorBrush(Color.FromArgb(0xFF, 0x77, 0x88, 0x99)),
		});
		background.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = TimeSpan.FromSeconds(0.6),
			Value = new SolidColorBrush(Color.FromArgb(0xFF, 0x42, 0xA5, 0xF5)),
		});
		background.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = TimeSpan.FromSeconds(1.2),
			Value = new SolidColorBrush(Color.FromArgb(0xFF, 0xEC, 0x40, 0x7A)),
		});
		Storyboard.SetTarget(background, CodeTarget);
		Storyboard.SetTargetProperty(background, "Background");

		var glyph = new ObjectAnimationUsingKeyFrames();
		glyph.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = GlyphWifi });
		glyph.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.FromSeconds(0.6), Value = GlyphChevronRight });
		glyph.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.FromSeconds(1.2), Value = GlyphCheckmark });
		Storyboard.SetTarget(glyph, CodeIcon);
		Storyboard.SetTargetProperty(glyph, "Glyph");

		new Storyboard { Children = { radius, background, glyph } }.Begin();
	}
}
