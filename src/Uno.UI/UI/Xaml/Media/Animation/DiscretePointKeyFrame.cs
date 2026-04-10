using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Discrete key frames like DiscretePointKeyFrame create sudden "jumps" between values (no Interpolation).
	/// In other words, the animated property does not change until the key frame's key time is reached
	/// at which point the animated property goes suddenly to the target value.
	/// </summary>
	public partial class DiscretePointKeyFrame : PointKeyFrame
	{
		/// <summary>
		/// Initializes a new instance of the DiscretePointKeyFrame class.
		/// </summary>
		public DiscretePointKeyFrame()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the DiscretePointKeyFrame class with the specified ending value.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		public DiscretePointKeyFrame(Point value)
			: base(value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DiscretePointKeyFrame class with the specified ending value and key time.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		/// <param name="keyTime">Key time for the key frame. The key time determines when the target value is reached which is also when the key frame ends.</param>
		public DiscretePointKeyFrame(Point value, KeyTime keyTime)
			: base(value, keyTime)
		{
		}

		internal override IEasingFunction GetEasingFunction()
		{
			return _easingFunction;
		}

		private static readonly IEasingFunction _easingFunction = new DiscreteDoubleKeyFrame.DiscreteDoubleKeyFrameEasingFunction();
	}
}
