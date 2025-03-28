using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Discrete key frames like DiscreteDoubleKeyFrame create sudden "jumps" between values (no Interpolation). 
	/// In other words, the animated property does not change until the key frame's key time is reached 
	/// at which point the animated property goes suddenly to the target value.
	/// </summary>
	public partial class DiscreteDoubleKeyFrame : DoubleKeyFrame
	{
		/// <summary>
		/// Initializes a new instance of the DiscreteDoubleKeyFrame class.
		/// </summary>
		public DiscreteDoubleKeyFrame()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the DiscreteDoubleKeyFrame class with the specified ending value.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		public DiscreteDoubleKeyFrame(double value)
			: base(value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DiscreteDoubleKeyFrame class with the specified ending value and key time.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		/// <param name="keyTime">Key time for the key frame. The key time determines when the target value is reached which is also when the key frame ends.</param>
		public DiscreteDoubleKeyFrame(double value, KeyTime keyTime)
			: base(value, keyTime)
		{
		}

		internal override IEasingFunction GetEasingFunction()
		{
			return _easingFunction;
		}

		private static readonly IEasingFunction _easingFunction = new DiscreteDoubleKeyFrameEasingFunction();

		/// <summary>
		/// This is a fake easing function that has no equivalent on GPU.
		/// We should implement DoubleAnimationUsingKeyFrames in a way to not use this.
		/// </summary>
		internal class DiscreteDoubleKeyFrameEasingFunction : IEasingFunction
		{
			public double Ease(double currentTime, double startValue, double finalValue, double duration)
			{
				if (currentTime < duration)
				{
					return startValue;
				}
				return finalValue;
			}
		}
	}
}
