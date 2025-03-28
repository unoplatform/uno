using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class LinearDoubleKeyFrame : DoubleKeyFrame
	{
		/// <summary>
		/// Initializes a new instance of the LinearDoubleKeyFrame class.
		/// </summary>
		public LinearDoubleKeyFrame()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the LinearDoubleKeyFrame class with the specified ending value.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		public LinearDoubleKeyFrame(double value)
			: base(value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the LinearDoubleKeyFrame class with the specified ending value and key time.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		/// <param name="keyTime">Key time for the key frame. The key time determines when the target value is reached which is also when the key frame ends.</param>
		public LinearDoubleKeyFrame(double value, KeyTime keyTime)
			: base(value, keyTime)
		{
		}

		internal override IEasingFunction GetEasingFunction()
		{
			// The default easing function is linear
			return null;
		}
	}
}
