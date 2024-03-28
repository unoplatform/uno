using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace Windows.UI.Xaml.Media.Animation
{
	internal class ColorValueAnimator : DisplayLinkValueAnimator
	{
		private readonly ColorOffset _from;
		private readonly ColorOffset _to;

		private ColorOffset[] _animatedValues;

		public ColorValueAnimator(ColorOffset from, ColorOffset to)
		{
			_to = to;
			_from = from;
		}

		protected override void PrebuildFrames(long numberOfFrames)
		{
			if (_animatedValues != null)
			{
				return;
			}

			_animatedValues = new ColorOffset[numberOfFrames];

			var by = _to - _from;
			var interpolation = (1f / numberOfFrames) * by;

			for (int f = 0; f < numberOfFrames; f++)
			{
				// TODO: apply easing, if any - https://github.com/unoplatform/uno/issues/2948

				_animatedValues[f] = _from + (f * interpolation);//frame value
			}
		}

		protected override object GetFinalUpdate() => _to;

		protected override object GetUpdate(int frame) => _animatedValues[frame];
	}
}
