using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Composition;

[Sample("Microsoft.UI.Composition")]
public sealed partial class Vector3KeyFrameAnimationSample : Page
{
	private readonly Visual _borderVisual;
	private readonly Visual _pageVisual;

	public Vector3KeyFrameAnimationSample()
	{
		this.InitializeComponent();
		_borderVisual = ElementCompositionPreview.GetElementVisual(border);
		_pageVisual = ElementCompositionPreview.GetElementVisual(this);
	}

	private void OnStartAnimation(object sender, RoutedEventArgs args)
	{
		var compositor = _borderVisual.Compositor;

		CompositionEasingFunction defaultEasingFunction = compositor.CreateCubicBezierEasingFunction(new(0.41f, 0.52f), new(0.0f, 0.94f));

		var easingFunction = easingFunctionBox.SelectedIndex switch
		{
			0 => defaultEasingFunction,
			1 => CompositionEasingFunction.CreateLinearEasingFunction(compositor),
			2 => CompositionEasingFunction.CreateBackEasingFunction(compositor, CompositionEasingFunctionMode.InOut, 0.9f),
			3 => CompositionEasingFunction.CreateBounceEasingFunction(compositor, CompositionEasingFunctionMode.Out, 3, 2),
			4 => CompositionEasingFunction.CreateCircleEasingFunction(compositor, CompositionEasingFunctionMode.InOut),
			5 => CompositionEasingFunction.CreateElasticEasingFunction(compositor, CompositionEasingFunctionMode.Out, 2, 1),
			6 => CompositionEasingFunction.CreateExponentialEasingFunction(compositor, CompositionEasingFunctionMode.Out, 6),
			7 => CompositionEasingFunction.CreatePowerEasingFunction(compositor, CompositionEasingFunctionMode.Out, 10),
			8 => CompositionEasingFunction.CreateSineEasingFunction(compositor, CompositionEasingFunctionMode.InOut),
			9 => CompositionEasingFunction.CreateStepEasingFunction(compositor, 3),
			_ => defaultEasingFunction
		};

		var animation = compositor.CreateVector3KeyFrameAnimation();
		var y = _borderVisual.Offset.Y;
		var maxX = _pageVisual.Size.X - _borderVisual.Size.X;
		if ((bool)hasFrameAtZeroCheckBox.IsChecked)
		{
			animation.InsertKeyFrame(0.0f, new Vector3(0.0f, y, 0.0f), easingFunction);
		}

		animation.InsertKeyFrame(0.5f, new Vector3(maxX / 4.0f, y, 0.0f), easingFunction);
		animation.InsertKeyFrame(0.6f, new Vector3(maxX / 4.0f, y, 0.0f), easingFunction);
		animation.InsertKeyFrame(1.0f, new Vector3(maxX, y, 0.0f), easingFunction);
		animation.Duration = TimeSpan.FromSeconds(2);
		animation.IterationCount = (int)iterationCountNumberBox.Value;
		animation.IterationBehavior = (bool)isForeverCheckBox.IsChecked ? AnimationIterationBehavior.Forever : AnimationIterationBehavior.Count;
		_borderVisual.StartAnimation("Offset", animation);
	}

	private void OnStopAnimation(object sender, RoutedEventArgs args)
	{
		_borderVisual.StopAnimation("Offset");
	}
}
