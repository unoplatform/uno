#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.UI.Composition
{
	public partial class KeyFrameAnimation : CompositionAnimation
	{
		internal KeyFrameAnimation() => throw new NotSupportedException();

		internal KeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		public AnimationStopBehavior StopBehavior { get; set; }

		public int IterationCount { get; set; }

		public AnimationIterationBehavior IterationBehavior { get; set; }

		public global::System.TimeSpan Duration { get; set; }

		public global::System.TimeSpan DelayTime { get; set; }

		public int KeyFrameCount { get; }

		public AnimationDirection Direction { get; set; }
	}
}
