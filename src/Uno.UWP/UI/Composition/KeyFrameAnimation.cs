using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Windows.UI.Composition
{
	public partial class KeyFrameAnimation : CompositionAnimation
	{
		private ImmutableArray<KeyFrame> _keys = ImmutableArray<KeyFrame>.Empty;

		internal KeyFrameAnimation() => throw new NotSupportedException();

		internal KeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		public AnimationStopBehavior StopBehavior { get; set; }

		public int IterationCount { get; set; }

		public AnimationIterationBehavior IterationBehavior { get; set; }

		public global::System.TimeSpan Duration { get; set; }

		public global::System.TimeSpan DelayTime { get; set; }

		public int KeyFrameCount { get; set; }

		public AnimationDirection Direction { get; set; }

		public virtual void InsertKeyFrame(float normalizedProgressKey, float value)
		{
			_keys = _keys.Add(new KeyFrame(normalizedProgressKey, value));
		}

		[global::Uno.NotImplemented]
		public virtual void InsertKeyFrame(float normalizedProgressKey, float value, global::Windows.UI.Composition.CompositionEasingFunction easingFunction)
		{

		}

		internal ImmutableArray<KeyFrame> Keys => _keys;

		internal class KeyFrame
		{
			public KeyFrame(float normalizedProgressKey, float value)
			{
				NormalizedProgressKey = normalizedProgressKey;
				Value = value;
			}

			public float NormalizedProgressKey { get; }
			public float Value { get; }
		}
	}
}
