using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class EasingPointKeyFrame : PointKeyFrame
	{
		/// <summary>
		/// Initializes a new instance of the EasingPointKeyFrame class.
		/// </summary>
		public EasingPointKeyFrame()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the EasingPointKeyFrame class with the specified Point value.
		/// </summary>
		/// <param name="value">The initial Point value.</param>
		public EasingPointKeyFrame(Point value)
			: base(value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EasingPointKeyFrame class with the specified Point value and key time.
		/// </summary>
		/// <param name="value">The initial Point value.</param>
		/// <param name="keyTime">The initial key time.</param>
		public EasingPointKeyFrame(Point value, KeyTime keyTime)
			: base(value, keyTime)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EasingPointKeyFrame class with the specified Point value, key time, and easing function.
		/// </summary>
		/// <param name="value">The initial Point value.</param>
		/// <param name="keyTime">The initial key time.</param>
		/// <param name="easingFunction">The easing function.</param>
		public EasingPointKeyFrame(Point value, KeyTime keyTime, EasingFunctionBase easingFunction)
			: base(value, keyTime)
		{
			EasingFunction = easingFunction;
		}

		/// <summary>
		/// Gets or sets the easing function applied to the key frame.
		/// </summary>
		public EasingFunctionBase EasingFunction
		{
			get { return (EasingFunctionBase)GetValue(EasingFunctionProperty); }
			set { this.SetValue(EasingFunctionProperty, value); }
		}
		public static DependencyProperty EasingFunctionProperty { get; } =
			DependencyProperty.Register("EasingFunction", typeof(EasingFunctionBase), typeof(EasingPointKeyFrame), new FrameworkPropertyMetadata(null));

		internal override IEasingFunction GetEasingFunction()
		{
			return EasingFunction;
		}
	}
}
