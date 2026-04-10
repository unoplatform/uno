using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class SplinePointKeyFrame : PointKeyFrame
	{
		/// <summary>
		/// Initializes a new instance of the SplinePointKeyFrame class.
		/// </summary>
		public SplinePointKeyFrame()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the SplinePointKeyFrame class with the specified ending value.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		public SplinePointKeyFrame(Point value)
			: base(value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the SplinePointKeyFrame class with the specified ending value and key time.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		/// <param name="keyTime">Key time for the key frame. The key time determines when the target value is reached which is also when the key frame ends.</param>
		public SplinePointKeyFrame(Point value, KeyTime keyTime)
			: base(value, keyTime)
		{
		}

		/// <summary>
		/// Initializes a new instance of the SplinePointKeyFrame class with the specified ending value, key time, and KeySpline.
		/// </summary>
		/// <param name="value">Ending value (also known as "target value") for the key frame.</param>
		/// <param name="keyTime">Key time for the key frame. The key time determines when the target value is reached which is also when the key frame ends.</param>
		/// <param name="keySpline">KeySpline for the key frame. The KeySpline represents a Bezier curve which defines animation progress of the key frame.</param>
		public SplinePointKeyFrame(Point value, KeyTime keyTime, KeySpline keySpline)
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
			DependencyProperty.Register("KeySpline", typeof(KeySpline), typeof(SplinePointKeyFrame), new FrameworkPropertyMetadata(new KeySpline()));

		internal override IEasingFunction GetEasingFunction()
		{
			return new SplineEasingFunction(KeySpline);
		}
	}
}
