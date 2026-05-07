using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Comprehensive showcase of PointAnimation and PointAnimationUsingKeyFrames including Discrete, Linear and Spline keyframe variants.")]
public sealed partial class PointAnimationShowcase : Page
{
	public PointAnimationShowcase()
	{
		this.InitializeComponent();
	}

	private void RunLinearPoint(object sender, RoutedEventArgs e)
	{
		LinearEllipseGeometry.Center = new Point(60, 100);

		var animation = new PointAnimation
		{
			From = new Point(60, 100),
			To = new Point(260, 100),
			Duration = new Duration(TimeSpan.FromSeconds(1.4)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
			EnableDependentAnimation = true,
		};
		Storyboard.SetTarget(animation, LinearEllipseGeometry);
		Storyboard.SetTargetProperty(animation, "Center");
		new Storyboard { Children = { animation } }.Begin();
	}

	private void StartGeometryLoop(object sender, RoutedEventArgs e)
	{
		GeometryCenterStoryboard.Begin();
	}

	private void StopGeometryLoop(object sender, RoutedEventArgs e)
	{
		GeometryCenterStoryboard.Stop();
	}

	private void RunKeyFrameComparison(object sender, RoutedEventArgs e)
	{
		DiscreteGeo.Center = new Point(20, 30);
		LinearGeo.Center = new Point(20, 60);
		SplineGeo.Center = new Point(20, 90);

		var duration = TimeSpan.FromSeconds(1.6);

		// Discrete
		var discrete = new PointAnimationUsingKeyFrames
		{
			Duration = new Duration(duration),
			EnableDependentAnimation = true,
		};
		discrete.KeyFrames.Add(new DiscretePointKeyFrame { KeyTime = TimeSpan.Zero, Value = new Point(20, 30) });
		discrete.KeyFrames.Add(new DiscretePointKeyFrame { KeyTime = TimeSpan.FromSeconds(0.4), Value = new Point(100, 30) });
		discrete.KeyFrames.Add(new DiscretePointKeyFrame { KeyTime = TimeSpan.FromSeconds(0.8), Value = new Point(180, 30) });
		discrete.KeyFrames.Add(new DiscretePointKeyFrame { KeyTime = TimeSpan.FromSeconds(1.2), Value = new Point(260, 30) });
		Storyboard.SetTarget(discrete, DiscreteGeo);
		Storyboard.SetTargetProperty(discrete, "Center");

		// Linear
		var linear = new PointAnimationUsingKeyFrames
		{
			Duration = new Duration(duration),
			EnableDependentAnimation = true,
		};
		linear.KeyFrames.Add(new LinearPointKeyFrame { KeyTime = TimeSpan.Zero, Value = new Point(20, 60) });
		linear.KeyFrames.Add(new LinearPointKeyFrame { KeyTime = duration, Value = new Point(280, 60) });
		Storyboard.SetTarget(linear, LinearGeo);
		Storyboard.SetTargetProperty(linear, "Center");

		// Spline
		var spline = new PointAnimationUsingKeyFrames
		{
			Duration = new Duration(duration),
			EnableDependentAnimation = true,
		};
		spline.KeyFrames.Add(new LinearPointKeyFrame { KeyTime = TimeSpan.Zero, Value = new Point(20, 90) });
		spline.KeyFrames.Add(new SplinePointKeyFrame
		{
			KeyTime = duration,
			Value = new Point(280, 90),
			KeySpline = new KeySpline
			{
				ControlPoint1 = new Point(0.8, 0),
				ControlPoint2 = new Point(0.2, 1),
			},
		});
		Storyboard.SetTarget(spline, SplineGeo);
		Storyboard.SetTargetProperty(spline, "Center");

		new Storyboard { Children = { discrete, linear, spline } }.Begin();
	}
}
