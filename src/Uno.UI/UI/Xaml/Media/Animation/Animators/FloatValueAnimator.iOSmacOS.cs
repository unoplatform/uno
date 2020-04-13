using CoreAnimation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Animates a float property using Xaml property setters.
	/// </summary>
	internal class FloatValueAnimator : DisplayLinkValueAnimator
	{
		private float _from;
		private float _to;

		private float[] _animatedValues;

		public FloatValueAnimator(float from, float to)
		{
			_to = to;
			_from = from;
		}

		protected override object GetFinalUpdate() => _to;

		protected override object GetUpdate(int frame) => _animatedValues[frame];

		protected override void PrebuildFrames(long numberOfFrames)
		{
			if (_animatedValues != null)
			{
				return;
			}

			_animatedValues = new float[numberOfFrames];

			var by = _to - _from; //how much to change the value
			var interpolation = by / numberOfFrames; //step size

			for (int f = 0; f < numberOfFrames; f++)
			{
				//Modifies the frame values of the animation depending on the easing function
				if (_easingFunction != null)
				{
					_animatedValues[f] = (float)_easingFunction.Ease(f, _from, _to, numberOfFrames);//frame value
				}
				else
				{
					//Regular Linear Function
					_animatedValues[f] = _from + (interpolation * f);//frame value
				}
			}
		}
	}
}
