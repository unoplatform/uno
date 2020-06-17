using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class RenderingLoopColorAnimator : RenderingLoopAnimator<ColorOffset>
	{
		public RenderingLoopColorAnimator(ColorOffset from, ColorOffset to) : base(from, to)
		{
		}

		protected override ColorOffset GetUpdatedValue(long frame, ColorOffset from, ColorOffset to)
		{
			if (_easing != null)
			{
				return _easing.Ease(frame, from, to, Duration);
			}

			var by = to - from;
			var currentFrame = (float)frame / Duration;
			var currentOffset = currentFrame * by;

			return from + currentOffset;
		}
	}
}
