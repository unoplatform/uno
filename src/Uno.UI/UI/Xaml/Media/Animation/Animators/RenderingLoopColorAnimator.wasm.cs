using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class RenderingLoopColorAnimator : RenderingLoopAnimator<ColorOffset>
	{
		public RenderingLoopColorAnimator(ColorOffset from, ColorOffset to) : base(from, to)
		{
		}

		protected override ColorOffset GetUpdatedValue(long frame, ColorOffset from, ColorOffset to)
		{
			var by = to - from;
			var currentFrame = (float)frame / Duration;
			var currentOffset = currentFrame * by;

			return from + currentOffset;
		}
	}
}
