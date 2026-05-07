using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Animations on each transform type — Translate, Scale, Rotate, Skew, CompositeTransform — plus a RenderTransformOrigin comparison.")]
public sealed partial class TransformAnimationShowcase : Page
{
	public TransformAnimationShowcase()
	{
		this.InitializeComponent();
	}

	private static void Run(DependencyObject target, string property, double from, double to, TimeSpan duration, EasingFunctionBase easing = null)
	{
		var animation = new DoubleAnimation
		{
			From = from,
			To = to,
			Duration = new Duration(duration),
			EasingFunction = easing,
			AutoReverse = true,
		};
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, property);
		new Storyboard { Children = { animation } }.Begin();
	}

	private void RunTranslate(object sender, RoutedEventArgs e)
	{
		var x = new DoubleAnimation
		{
			From = 0,
			To = 200,
			Duration = new Duration(TimeSpan.FromSeconds(1.0)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
			AutoReverse = true,
		};
		Storyboard.SetTarget(x, TranslateTarget);
		Storyboard.SetTargetProperty(x, "X");

		var y = new DoubleAnimation
		{
			From = 0,
			To = -30,
			Duration = new Duration(TimeSpan.FromSeconds(1.0)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
			AutoReverse = true,
		};
		Storyboard.SetTarget(y, TranslateTarget);
		Storyboard.SetTargetProperty(y, "Y");

		new Storyboard { Children = { x, y } }.Begin();
	}

	private void RunScale(object sender, RoutedEventArgs e)
	{
		Run(ScaleTarget, "ScaleX", 1, 2.0, TimeSpan.FromSeconds(0.6), new BackEase { EasingMode = EasingMode.EaseInOut });
		Run(ScaleTarget, "ScaleY", 1, 2.0, TimeSpan.FromSeconds(0.6), new BackEase { EasingMode = EasingMode.EaseInOut });
	}

	private void RunRotate(object sender, RoutedEventArgs e)
	{
		var animation = new DoubleAnimation
		{
			From = 0,
			To = 360,
			Duration = new Duration(TimeSpan.FromSeconds(1.0)),
		};
		RotateTarget.CenterX = 40;
		RotateTarget.CenterY = 40;
		Storyboard.SetTarget(animation, RotateTarget);
		Storyboard.SetTargetProperty(animation, "Angle");
		new Storyboard { Children = { animation } }.Begin();
	}

	private void RunSkew(object sender, RoutedEventArgs e)
	{
		Run(SkewTarget, "AngleX", 0, 30, TimeSpan.FromSeconds(0.5), new SineEase { EasingMode = EasingMode.EaseInOut });
		Run(SkewTarget, "AngleY", 0, 15, TimeSpan.FromSeconds(0.5), new SineEase { EasingMode = EasingMode.EaseInOut });
	}

	private void StartComposite(object sender, RoutedEventArgs e)
	{
		CompositeStoryboard.Begin();
	}

	private void StopComposite(object sender, RoutedEventArgs e)
	{
		CompositeStoryboard.Stop();
	}

	private void RunOriginCompare(object sender, RoutedEventArgs e)
	{
		var topLeft = new DoubleAnimation
		{
			From = 0,
			To = 360,
			Duration = new Duration(TimeSpan.FromSeconds(1.5)),
		};
		Storyboard.SetTarget(topLeft, OriginTopLeftTarget);
		Storyboard.SetTargetProperty(topLeft, "Angle");

		var center = new DoubleAnimation
		{
			From = 0,
			To = 360,
			Duration = new Duration(TimeSpan.FromSeconds(1.5)),
		};
		Storyboard.SetTarget(center, OriginCenterTarget);
		Storyboard.SetTargetProperty(center, "Angle");

		new Storyboard { Children = { topLeft, center } }.Begin();
	}
}
