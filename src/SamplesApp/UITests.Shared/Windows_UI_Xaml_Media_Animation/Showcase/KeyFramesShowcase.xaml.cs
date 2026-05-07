using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Side-by-side comparison of Discrete, Linear, Spline and Easing keyframe variants for both DoubleAnimationUsingKeyFrames and ColorAnimationUsingKeyFrames.")]
public sealed partial class KeyFramesShowcase : Page
{
	public KeyFramesShowcase()
	{
		this.InitializeComponent();
	}

	private void RunDoubleKeyFrames(object sender, RoutedEventArgs e)
	{
		DiscreteTransform.X = 0;
		LinearTransform.X = 0;
		SplineTransform.X = 0;
		EasingTransform.X = 0;
		DoubleKeyFrameStoryboard.Begin();
	}

	private void RunColorKeyFrames(object sender, RoutedEventArgs e)
	{
		ColorKeyFrameStoryboard.Begin();
	}
}
