using System;
using System.Globalization;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class FadeOutThemeAnimation : Timeline, ITimeline, IAnimation<float>
	{
		private readonly AnimationImplementation<float> _animationImplementation;

		public FadeOutThemeAnimation()
		{
			_animationImplementation = new AnimationImplementation<float>(this);
		}

		public static DependencyProperty TargetNameProperty { get; } = DependencyProperty.Register(
			"TargetName", typeof(string), typeof(FadeOutThemeAnimation), new FrameworkPropertyMetadata(string.Empty));

		public string TargetName
		{
			get => (string)GetValue(TargetNameProperty);
			set => SetValue(TargetNameProperty, value);
		}

		private protected override void InitTarget()
		{
			var target = NameScope.GetNameScope(this)?.FindName(TargetName);
			if (target is DependencyObject depObj)
			{
				Storyboard.SetTarget(this, depObj);
			}
		}

		float? IAnimation<float>.To => 0f;

		float? IAnimation<float>.From => null;

		float? IAnimation<float>.By => null;

		bool IAnimation<float>.EnableDependentAnimation => false;

		IEasingFunction IAnimation<float>.EasingFunction => null;

		float IAnimation<float>.Subtract(float minuend, float subtrahend) => minuend - subtrahend;

		float IAnimation<float>.Add(float first, float second) => first + second;

		float IAnimation<float>.Convert(object value) => Convert.ToSingle(value, CultureInfo.InvariantCulture);

		float IAnimation<float>.Multiply(float multiplier, float t) => multiplier * t;

		void ITimeline.Begin() => _animationImplementation.Begin();

		void ITimeline.Stop() => _animationImplementation.Stop();

		void ITimeline.Resume() => _animationImplementation.Resume();

		void ITimeline.Pause() => _animationImplementation.Pause();

		void ITimeline.Seek(TimeSpan offset) => _animationImplementation.Seek(offset);

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset) => _animationImplementation.SeekAlignedToLastTick(offset);

		void ITimeline.SkipToFill() => _animationImplementation.SkipToFill();

		void ITimeline.Deactivate() => _animationImplementation.Deactivate();

		private protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_animationImplementation.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}
