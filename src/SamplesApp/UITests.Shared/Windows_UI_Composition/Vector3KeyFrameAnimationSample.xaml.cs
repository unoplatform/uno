using System;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Composition;

[Sample("Windows.UI.Composition")]
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
		var animation = _borderVisual.Compositor.CreateVector3KeyFrameAnimation();
		var y = _borderVisual.Offset.Y;
		var maxX = _pageVisual.Size.X - _borderVisual.Size.X;
		if ((bool)hasFrameAtZeroCheckBox.IsChecked)
		{
			animation.InsertKeyFrame(0.0f, new Vector3(0.0f, y, 0.0f));
		}

		animation.InsertKeyFrame(0.5f, new Vector3(maxX / 4.0f, y, 0.0f));
		animation.InsertKeyFrame(0.6f, new Vector3(maxX / 4.0f, y, 0.0f));
		animation.InsertKeyFrame(1.0f, new Vector3(maxX, y, 0.0f));
		animation.Duration = TimeSpan.FromSeconds(1);
		animation.IterationCount = (int)iterationCountNumberBox.Value;
		animation.IterationBehavior = (bool)isForeverCheckBox.IsChecked ? AnimationIterationBehavior.Forever : AnimationIterationBehavior.Count;
		_borderVisual.StartAnimation("Offset", animation);
	}

	private void OnStopAnimation(object sender, RoutedEventArgs args)
	{
		_borderVisual.StopAnimation("Offset");
	}
}
