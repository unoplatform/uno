#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno;

namespace Windows.UI.Composition;

public partial class ScalarKeyFrameAnimation : KeyFrameAnimation
{
	internal ImmutableArray<KeyFrame> Keys { get; private set; } = ImmutableArray<KeyFrame>.Empty;

	public void InsertKeyFrame(float normalizedProgressKey, float value)
	{
		Keys = Keys.Add(new KeyFrame(normalizedProgressKey, value));
	}

	private protected override int KeyFrameCountCore => Keys.Length;

	[NotImplemented]
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
