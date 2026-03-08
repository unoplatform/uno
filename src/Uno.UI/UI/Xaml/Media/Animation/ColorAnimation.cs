using System;
using Uno;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ColorAnimation : Timeline, ITimeline, IAnimation<ColorOffset>
	{
		private readonly AnimationImplementation<ColorOffset> _animationImplementation;

		public ColorAnimation()
		{
			_animationImplementation = new AnimationImplementation<ColorOffset>(this);
		}

		public Color? To
		{
			get => (Color?)this.GetValue(ToProperty);
			set
			{
				this.SetValue(ToProperty, value);
			}
		}

		public Color? From
		{
			get
			{
				return (Color?)this.GetValue(FromProperty);
			}
			set
			{
				this.SetValue(FromProperty, value);
			}
		}

		public bool EnableDependentAnimation
		{
			get
			{
				return (bool)this.GetValue(EnableDependentAnimationProperty);
			}
			set
			{
				this.SetValue(EnableDependentAnimationProperty, value);
			}
		}

		bool IAnimation<ColorOffset>.EnableDependentAnimation => EnableDependentAnimation;

		public Color? By
		{
			get
			{
				return (Color?)this.GetValue(ByProperty);
			}
			set
			{
				this.SetValue(ByProperty, value);
			}
		}

		public static DependencyProperty ByProperty { get; } =
		DependencyProperty.Register(
			"By", typeof(Color?),
			typeof(ColorAnimation),
			new FrameworkPropertyMetadata(default(Color?)));

		public static DependencyProperty EnableDependentAnimationProperty { get; } =
		DependencyProperty.Register(
			"EnableDependentAnimation", typeof(bool),
			typeof(ColorAnimation),
			new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty FromProperty { get; } =
		DependencyProperty.Register(
			"From", typeof(Color?),
			typeof(ColorAnimation),
			new FrameworkPropertyMetadata(default(Color?)));

		public static DependencyProperty ToProperty { get; } =
		DependencyProperty.Register(
			"To", typeof(Color?),
			typeof(ColorAnimation),
			new FrameworkPropertyMetadata(default(Color?)));

		ColorOffset? IAnimation<ColorOffset>.To => (ColorOffset?)To;

		ColorOffset? IAnimation<ColorOffset>.From => (ColorOffset?)From;

		ColorOffset? IAnimation<ColorOffset>.By => (ColorOffset?)By;

		[NotImplemented]
		IEasingFunction IAnimation<ColorOffset>.EasingFunction => null;

		void ITimeline.Begin() => _animationImplementation.Begin();

		void ITimeline.Stop() => _animationImplementation.Stop();

		void ITimeline.Resume() => _animationImplementation.Resume();

		void ITimeline.Pause() => _animationImplementation.Pause();

		void ITimeline.Seek(TimeSpan offset) => _animationImplementation.Seek(offset);

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset) => _animationImplementation.SeekAlignedToLastTick(offset);

		void ITimeline.SkipToFill() => _animationImplementation.SkipToFill();

		void ITimeline.Deactivate() => _animationImplementation.Deactivate();

		void ITimeline.BeginReversed() => _animationImplementation.BeginReversed();

		void ITimeline.SkipToFillReversed() => _animationImplementation.SkipToFillReversed();

		/// <summary>
		/// Dispose the Double animation.
		/// </summary>
		private protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_animationImplementation.Dispose();
			}

			base.Dispose(disposing);
		}

		ColorOffset IAnimation<ColorOffset>.Subtract(ColorOffset minuend, ColorOffset subtrahend) => minuend - subtrahend;

		ColorOffset IAnimation<ColorOffset>.Add(ColorOffset first, ColorOffset second) => first + second;

		ColorOffset IAnimation<ColorOffset>.Multiply(float multiplier, ColorOffset color) => multiplier * color;

		ColorOffset IAnimation<ColorOffset>.Convert(object value)
		{
			if (value is string s)
			{
				return (ColorOffset)Colors.Parse(s);
			}
			else if (value is Color c)
			{
				return (ColorOffset)c;
			}

			// TODO: handle int?
			return default(ColorOffset);
		}
	}
}
