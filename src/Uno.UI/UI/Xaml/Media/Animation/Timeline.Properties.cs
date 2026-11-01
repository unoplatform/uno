#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	private List<EventHandler<object>>? _completedHandlers;

	/// <summary>
	/// Occurs when the Storyboard object has completed playing.
	/// </summary>
	public event EventHandler<object> Completed
	{
		add
		{
			if (value is not null)
			{
				(_completedHandlers ??= new()).Add(value);
				m_completedHandlerRegisteredCount++;
			}
		}
		remove
		{
			if (_completedHandlers?.Remove(value) == true)
			{
				m_completedHandlerRegisteredCount--;
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that declares whether animations that are considered dependent animations should be
	/// permitted to use the animation system.
	/// </summary>
	public static bool AllowDependentAnimations
	{
		get => s_allowDependentAnimations;
		set => s_allowDependentAnimations = value;
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the timeline plays in reverse after it completes a forward iteration.
	/// </summary>
	public bool AutoReverse
	{
		get => (bool)GetValue(AutoReverseProperty);
		set => SetValue(AutoReverseProperty, value);
	}

	/// <summary>
	/// Identifies the AutoReverse dependency property.
	/// </summary>
	public static DependencyProperty AutoReverseProperty { get; } =
		DependencyProperty.Register(
			nameof(AutoReverse),
			typeof(bool),
			typeof(Timeline),
			new FrameworkPropertyMetadata(BoolBoxes.False, (s, e) => ((Timeline)s).m_fAutoReverse = (bool)e.NewValue));

	/// <summary>
	/// Gets or sets the time at which this Timeline should begin.
	/// </summary>
	public TimeSpan? BeginTime
	{
		get => (TimeSpan?)GetValue(BeginTimeProperty);
		set => SetValue(BeginTimeProperty, value);
	}

	/// <summary>
	/// Identifies the BeginTime dependency property.
	/// </summary>
	public static DependencyProperty BeginTimeProperty { get; } =
		DependencyProperty.Register(
			nameof(BeginTime),
			typeof(TimeSpan?),
			typeof(Timeline),
			new FrameworkPropertyMetadata(TimeSpan.Zero, (s, e) => ((Timeline)s).m_pBeginTime = (TimeSpan?)e.NewValue));

	/// <summary>
	/// Gets or sets the length of time for which this timeline plays, not counting repetitions.
	/// </summary>
	public Duration Duration
	{
		get => (Duration)GetValue(DurationProperty);
		set => SetValue(DurationProperty, value);
	}

	/// <summary>
	/// Identifies the Duration dependency property.
	/// </summary>
	public static DependencyProperty DurationProperty { get; } =
		DependencyProperty.Register(
			nameof(Duration),
			typeof(Duration),
			typeof(Timeline),
			new FrameworkPropertyMetadata(Duration.Automatic, (s, e) => ((Timeline)s).m_duration = (Duration)e.NewValue));

	/// <summary>
	/// Gets or sets a value that specifies how the animation behaves after it reaches the end of its active period.
	/// </summary>
	public FillBehavior FillBehavior
	{
		get => (FillBehavior)GetValue(FillBehaviorProperty);
		set => SetValue(FillBehaviorProperty, value);
	}

	/// <summary>
	/// Identifies the FillBehavior dependency property.
	/// </summary>
	public static DependencyProperty FillBehaviorProperty { get; } =
		DependencyProperty.Register(
			nameof(FillBehavior),
			typeof(FillBehavior),
			typeof(Timeline),
			new FrameworkPropertyMetadata(FillBehavior.HoldEnd, (s, e) => ((Timeline)s).m_fillBehavior = (FillBehavior)e.NewValue));

	/// <summary>
	/// Gets or sets the repeating behavior of this timeline.
	/// </summary>
	public RepeatBehavior RepeatBehavior
	{
		get => (RepeatBehavior)GetValue(RepeatBehaviorProperty);
		set => SetValue(RepeatBehaviorProperty, value);
	}

	/// <summary>
	/// Identifies the RepeatBehavior dependency property.
	/// </summary>
	public static DependencyProperty RepeatBehaviorProperty { get; } =
		DependencyProperty.Register(
			nameof(RepeatBehavior),
			typeof(RepeatBehavior),
			typeof(Timeline),
			new FrameworkPropertyMetadata(new RepeatBehavior(1), (s, e) => ((Timeline)s).m_repeatBehavior = (RepeatBehavior)e.NewValue));

	/// <summary>
	/// Gets or sets the rate, relative to its parent, at which time progresses for this Timeline.
	/// </summary>
	public double SpeedRatio
	{
		get => (double)GetValue(SpeedRatioProperty);
		set => SetValue(SpeedRatioProperty, value);
	}

	/// <summary>
	/// Identifies the SpeedRatio dependency property.
	/// </summary>
	public static DependencyProperty SpeedRatioProperty { get; } =
		DependencyProperty.Register(
			nameof(SpeedRatio),
			typeof(double),
			typeof(Timeline),
			new FrameworkPropertyMetadata(DoubleBoxes.One, (s, e) => ((Timeline)s).OnSpeedRatioChanged((double)e.NewValue)));
}
