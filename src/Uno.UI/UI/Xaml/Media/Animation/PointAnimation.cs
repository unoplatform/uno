// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/PointAnimation.cpp

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class PointAnimation : Timeline, ITimeline, IAnimation<Point>
	{
		private readonly AnimationImplementation<Point> _animationImplementation;

		public PointAnimation()
		{
			_animationImplementation = new AnimationImplementation<Point>(this);
		}

		/// <summary>
		/// Returns 1 second for Automatic duration (WinUI NULL_DURATION_DEFAULT).
		/// MUX: CAnimation::GetNaturalDuration returns 1.0f when Duration is not set.
		/// </summary>
		internal override TimeSpan GetCalculatedDuration()
		{
			if (Duration.Type == DurationType.Automatic)
			{
				return TimeSpan.FromSeconds(1);
			}
			return base.GetCalculatedDuration();
		}

		public Point? To
		{
			get => (Point?)GetValue(ToProperty);
			set => SetValue(ToProperty, value);
		}

		public static DependencyProperty ToProperty { get; } =
			DependencyProperty.Register(nameof(To), typeof(Point?), typeof(PointAnimation), new FrameworkPropertyMetadata(default(Point?)));

		public Point? From
		{
			get => (Point?)GetValue(FromProperty);
			set => SetValue(FromProperty, value);
		}

		public static DependencyProperty FromProperty { get; } =
			DependencyProperty.Register(nameof(From), typeof(Point?), typeof(PointAnimation), new FrameworkPropertyMetadata(default(Point?)));

		public Point? By
		{
			get => (Point?)GetValue(ByProperty);
			set => SetValue(ByProperty, value);
		}

		public static DependencyProperty ByProperty { get; } =
			DependencyProperty.Register(nameof(By), typeof(Point?), typeof(PointAnimation), new FrameworkPropertyMetadata(default(Point?)));

		public bool EnableDependentAnimation
		{
			get => (bool)GetValue(EnableDependentAnimationProperty);
			set => SetValue(EnableDependentAnimationProperty, value);
		}

		bool IAnimation<Point>.EnableDependentAnimation => EnableDependentAnimation;

		public static DependencyProperty EnableDependentAnimationProperty { get; } =
			DependencyProperty.Register(nameof(EnableDependentAnimation), typeof(bool), typeof(PointAnimation), new FrameworkPropertyMetadata(false));

		public EasingFunctionBase EasingFunction
		{
			get => (EasingFunctionBase)GetValue(EasingFunctionProperty);
			set => SetValue(EasingFunctionProperty, value);
		}

		IEasingFunction IAnimation<Point>.EasingFunction => EasingFunction;

		public static DependencyProperty EasingFunctionProperty { get; } =
			DependencyProperty.Register(nameof(EasingFunction), typeof(EasingFunctionBase), typeof(PointAnimation), new FrameworkPropertyMetadata(default(EasingFunctionBase)));

		Point? IAnimation<Point>.To => To;

		Point? IAnimation<Point>.From => From;

		Point? IAnimation<Point>.By => By;

		void ITimeline.Begin()
		{
#if __SKIA__
			if (IsParentStoryboardRegistered())
			{
				_isTimeManagerDriven = true;
				_tmCompletedEventFired = false;
				_tmInitialized = false;
				State = TimelineState.Active;
				return;
			}
#endif
			_animationImplementation.Begin();
		}

		void ITimeline.Stop()
		{
#if __SKIA__
			if (_isTimeManagerDriven)
			{
				_isTimeManagerDriven = false;
				ResetTimeManagerState();
				ClearValue();
				State = TimelineState.Stopped;
				return;
			}
#endif
			_animationImplementation.Stop();
		}

		void ITimeline.Resume()
		{
#if __SKIA__
			if (_isTimeManagerDriven)
			{
				State = TimelineState.Active;
				return;
			}
#endif
			_animationImplementation.Resume();
		}

		void ITimeline.Pause()
		{
#if __SKIA__
			if (_isTimeManagerDriven)
			{
				State = TimelineState.Paused;
				return;
			}
#endif
			_animationImplementation.Pause();
		}

		void ITimeline.Seek(TimeSpan offset) => _animationImplementation.Seek(offset);

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset) => _animationImplementation.SeekAlignedToLastTick(offset);

		void ITimeline.SkipToFill()
		{
#if __SKIA__
			if (_isTimeManagerDriven)
			{
				_isTimeManagerDriven = false;
				ResetTimeManagerState();
				var toValue = To.HasValue ? To.Value : _tmToValue;
				SetValue(toValue);
				State = TimelineState.Filling;
				OnCompleted();
				return;
			}
#endif
			_animationImplementation.SkipToFill();
		}

		void ITimeline.Deactivate()
		{
#if __SKIA__
			if (_isTimeManagerDriven)
			{
				_isTimeManagerDriven = false;
				ResetTimeManagerState();
				State = TimelineState.Stopped;
				return;
			}
#endif
			_animationImplementation.Deactivate();
		}

		private protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_animationImplementation.Dispose();
			}

			base.Dispose(disposing);
		}

		Point IAnimation<Point>.Subtract(Point minuend, Point subtrahend) =>
			new Point(minuend.X - subtrahend.X, minuend.Y - subtrahend.Y);

		Point IAnimation<Point>.Add(Point first, Point second) =>
			new Point(first.X + second.X, first.Y + second.Y);

		Point IAnimation<Point>.Multiply(float multiplier, Point t) =>
			new Point(multiplier * t.X, multiplier * t.Y);

		Point IAnimation<Point>.Convert(object value)
		{
			if (value is Point p)
			{
				return p;
			}
			return default;
		}
	}
}
