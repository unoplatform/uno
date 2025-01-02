#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno;

namespace Windows.UI.Composition
{
	public partial class KeyFrameAnimation : CompositionAnimation
	{
		private protected IKeyFrameEvaluator? _keyframeEvaluator;

		internal KeyFrameAnimation() => throw new NotSupportedException();

		internal KeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		internal override bool IsTrackedByCompositor => true;

		[NotImplemented]
		public AnimationStopBehavior StopBehavior { get; set; }

		public int IterationCount { get; set; } = 1;

		public AnimationIterationBehavior IterationBehavior { get; set; }

		public TimeSpan Duration { get; set; }

		[NotImplemented]
		public TimeSpan DelayTime { get; set; }

		public int KeyFrameCount => KeyFrameCountCore;

		private protected virtual int KeyFrameCountCore { get; }

		[NotImplemented]
		public AnimationDirection Direction { get; set; }

		internal override object Evaluate()
		{
			var (value, shouldStop) = _keyframeEvaluator!.Evaluate();
			if (shouldStop)
			{
				Stop();
			}

			return value;
		}
	}
}
