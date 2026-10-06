using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Comprehensive showcase of DoubleAnimation: From/To/By, BeginTime, AutoReverse, RepeatBehavior, FillBehavior, SpeedRatio.")]
public sealed partial class DoubleAnimationShowcase : Page
{
	private Storyboard _repeatForeverStoryboard;

	public DoubleAnimationShowcase()
	{
		this.InitializeComponent();
	}

	private static DoubleAnimation Animation(double from, double to, TimeSpan duration)
	{
		return new DoubleAnimation
		{
			From = from,
			To = to,
			Duration = new Duration(duration),
			EnableDependentAnimation = true,
		};
	}

	private static Storyboard Run(DependencyObject target, string property, DoubleAnimation animation)
	{
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, property);
		var storyboard = new Storyboard { Children = { animation } };
		storyboard.Begin();
		return storyboard;
	}

	private void RunFromTo(object sender, RoutedEventArgs e)
	{
		FromToBar.Width = 50;
		Run(FromToBar, "Width", Animation(50, 300, TimeSpan.FromSeconds(1.5)));
	}

	private void RunBy(object sender, RoutedEventArgs e)
	{
		var animation = new DoubleAnimation
		{
			By = 50,
			Duration = new Duration(TimeSpan.FromSeconds(0.6)),
			EnableDependentAnimation = true,
		};
		Run(ByBar, "Width", animation);
	}

	private void RunBeginTime(object sender, RoutedEventArgs e)
	{
		void Stagger(FrameworkElement target, double offsetSeconds)
		{
			target.Width = 40;
			var animation = new DoubleAnimation
			{
				From = 40,
				To = 280,
				Duration = new Duration(TimeSpan.FromSeconds(1.0)),
				BeginTime = TimeSpan.FromSeconds(offsetSeconds),
				EnableDependentAnimation = true,
			};
			Run(target, "Width", animation);
		}

		Stagger(StaggerA, 0);
		Stagger(StaggerB, 0.4);
		Stagger(StaggerC, 0.8);
	}

	private void RunAutoReverse(object sender, RoutedEventArgs e)
	{
		AutoReverseTransform.X = 0;
		var animation = new DoubleAnimation
		{
			From = 0,
			To = 250,
			Duration = new Duration(TimeSpan.FromSeconds(1.0)),
			AutoReverse = true,
		};
		Run(AutoReverseTransform, "X", animation);
	}

	private void RunRepeatBehavior(object sender, RoutedEventArgs e)
	{
		RepeatCountTransform.X = 0;
		RepeatDurationTransform.X = 0;
		RepeatForeverTransform.X = 0;

		_repeatForeverStoryboard?.Stop();

		var count = new DoubleAnimation
		{
			From = 0,
			To = 250,
			Duration = new Duration(TimeSpan.FromSeconds(1)),
			RepeatBehavior = new RepeatBehavior(3),
		};
		Run(RepeatCountTransform, "X", count);

		var duration = new DoubleAnimation
		{
			From = 0,
			To = 250,
			Duration = new Duration(TimeSpan.FromSeconds(1)),
			RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(2)),
		};
		Run(RepeatDurationTransform, "X", duration);

		var forever = new DoubleAnimation
		{
			From = 0,
			To = 250,
			Duration = new Duration(TimeSpan.FromSeconds(1)),
			RepeatBehavior = RepeatBehavior.Forever,
		};
		_repeatForeverStoryboard = Run(RepeatForeverTransform, "X", forever);
	}

	private void StopRepeatForever(object sender, RoutedEventArgs e)
	{
		_repeatForeverStoryboard?.Stop();
		_repeatForeverStoryboard = null;
	}

	private void RunFillBehavior(object sender, RoutedEventArgs e)
	{
		HoldEndTransform.X = 0;
		StopTransform.X = 0;

		var hold = new DoubleAnimation
		{
			From = 0,
			To = 250,
			Duration = new Duration(TimeSpan.FromSeconds(1.0)),
			FillBehavior = FillBehavior.HoldEnd,
		};
		Run(HoldEndTransform, "X", hold);

		var stop = new DoubleAnimation
		{
			From = 0,
			To = 250,
			Duration = new Duration(TimeSpan.FromSeconds(1.0)),
			FillBehavior = FillBehavior.Stop,
		};
		Run(StopTransform, "X", stop);
	}

	private void RunSpeedRatio(object sender, RoutedEventArgs e)
	{
		SpeedSlowTransform.X = 0;
		SpeedNormalTransform.X = 0;
		SpeedFastTransform.X = 0;

		void Animate(DependencyObject target, double speed)
		{
			var animation = new DoubleAnimation
			{
				From = 0,
				To = 250,
				Duration = new Duration(TimeSpan.FromSeconds(1.0)),
				SpeedRatio = speed,
			};
			Run(target, "X", animation);
		}

		Animate(SpeedSlowTransform, 0.25);
		Animate(SpeedNormalTransform, 1.0);
		Animate(SpeedFastTransform, 4.0);
	}
}
