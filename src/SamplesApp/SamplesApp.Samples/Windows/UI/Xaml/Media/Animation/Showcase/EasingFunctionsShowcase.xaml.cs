using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "All built-in easing functions side-by-side: Back, Bounce, Circle, Cubic, Elastic, Exponential, Linear, Power, Quadratic, Quartic, Quintic, Sine.")]
public sealed partial class EasingFunctionsShowcase : Page
{
	public EasingFunctionsShowcase()
	{
		this.InitializeComponent();
	}

	private IEnumerable<(TranslateTransform Transform, EasingFunctionBase Easing)> Lanes(EasingMode mode)
	{
		yield return (BackTransform, new BackEase { EasingMode = mode, Amplitude = 0.6 });
		yield return (BounceTransform, new BounceEase { EasingMode = mode, Bounces = 3, Bounciness = 2 });
		yield return (CircleTransform, new CircleEase { EasingMode = mode });
		yield return (CubicTransform, new CubicEase { EasingMode = mode });
		yield return (ElasticTransform, new ElasticEase { EasingMode = mode, Oscillations = 2, Springiness = 3 });
		yield return (ExponentialTransform, new ExponentialEase { EasingMode = mode, Exponent = 4 });
		yield return (LinearTransform, null); // no easing - linear baseline
		yield return (PowerTransform, new PowerEase { EasingMode = mode, Power = 3 });
		yield return (QuadraticTransform, new QuadraticEase { EasingMode = mode });
		yield return (QuarticTransform, new QuarticEase { EasingMode = mode });
		yield return (QuinticTransform, new QuinticEase { EasingMode = mode });
		yield return (SineTransform, new SineEase { EasingMode = mode });
	}

	private void Run(EasingMode mode)
	{
		var storyboard = new Storyboard();
		foreach (var (transform, easing) in Lanes(mode))
		{
			transform.X = 0;
			var animation = new DoubleAnimation
			{
				From = 0,
				To = 320,
				Duration = new Duration(TimeSpan.FromSeconds(1.5)),
				EasingFunction = easing,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, "X");
			storyboard.Children.Add(animation);
		}
		storyboard.Begin();
	}

	private void RunEaseIn(object sender, RoutedEventArgs e) => Run(EasingMode.EaseIn);
	private void RunEaseOut(object sender, RoutedEventArgs e) => Run(EasingMode.EaseOut);
	private void RunEaseInOut(object sender, RoutedEventArgs e) => Run(EasingMode.EaseInOut);
}
