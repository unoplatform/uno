#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Windows.UI.Composition
{
	public partial class ScalarKeyFrameAnimation : global::Windows.UI.Composition.KeyFrameAnimation
	{
		internal ImmutableArray<KeyFrame> Keys { get; private set; } = ImmutableArray<KeyFrame>.Empty;

		internal ScalarKeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		public void InsertKeyFrame(float normalizedProgressKey, float value)
		{
			Keys = Keys.Add(new KeyFrame(normalizedProgressKey, value));
		}

		[global::Uno.NotImplemented]
		public void InsertKeyFrame(float normalizedProgressKey, float value, global::Windows.UI.Composition.CompositionEasingFunction easingFunction)
		{

		}

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
