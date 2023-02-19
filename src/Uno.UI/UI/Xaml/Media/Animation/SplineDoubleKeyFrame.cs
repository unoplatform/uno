using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class SplineDoubleKeyFrame : DoubleKeyFrame
	{
		/// <summary>
		/// Initializes a new instance of the SplineDoubleKeyFrame class.
		/// </summary>
		public SplineDoubleKeyFrame()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the SplineDoubleKeyFrame class with the specified ending value.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		public SplineDoubleKeyFrame(double value)
			: base(value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the SplineDoubleKeyFrame class with the specified ending value and key time.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		/// <param name="keyTime">Key time for the key frame. The key time determines when the target value is reached which is also when the key frame ends.</param>
		public SplineDoubleKeyFrame(double value, KeyTime keyTime)
			: base(value, keyTime)
		{
		}

		/// <summary>
		/// Initializes a new instance of the SplineDoubleKeyFrame class with the specified ending value, key time, and KeySpline.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		/// <param name="keyTime">Key time for the key frame. The key time determines when the target value is reached which is also when the key frame ends.</param>
		/// <param name="keySpline">KeySpline for the key frame. The KeySpline represents a Bezier curve which defines animation progress of the key frame.</param>
		public SplineDoubleKeyFrame(double value, KeyTime keyTime, KeySpline keySpline)
			: base(value, keyTime)
		{
			KeySpline = keySpline;
		}


		/// <summary>
		/// The two control points that specify the cubic Bezier curve which defines the progress of the key frame.
		/// </summary>
		public KeySpline KeySpline
		{
			get { return (KeySpline)this.GetValue(KeySplineProperty); }
			set { this.SetValue(KeySplineProperty, value); }
		}
		public static DependencyProperty KeySplineProperty { get; } =
			DependencyProperty.Register("KeySpline", typeof(KeySpline), typeof(SplineDoubleKeyFrame), new FrameworkPropertyMetadata(new KeySpline()));

		internal override IEasingFunction GetEasingFunction()
		{
			return new SplineEasingFunction(KeySpline);
		}
	}
}
