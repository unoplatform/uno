using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Private.Infrastructure;

namespace UITests.Windows_UI_Xaml_Media_Animation;

[Sample("Animations", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "The circle should be animating")]
public sealed partial class Canvas_DependentAnimation : Page
{
	public Canvas_DependentAnimation()
	{
		this.InitializeComponent();

		var doubleAnimation = new DoubleAnimation
		{
			From = 0,
			To = 250,
			Duration = TimeSpan.Parse("0:0:2"),
			RepeatBehavior = RepeatBehavior.Forever,
		};
		Storyboard.SetTargetName(doubleAnimation, "bouncingBall");
		Storyboard.SetTargetProperty(doubleAnimation, "(Windows.UI.Xaml.Controls:Canvas.Top)");
		Storyboard.SetTarget(doubleAnimation, bouncingBall);
		var storyboard = new Storyboard
		{
			Children =
				{
					doubleAnimation
				}
		};

		_ = UnitTestDispatcherCompat.From(bouncingBall).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => storyboard.Begin());
	}
}
