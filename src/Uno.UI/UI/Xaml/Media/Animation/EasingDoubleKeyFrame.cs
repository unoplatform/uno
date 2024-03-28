using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class EasingDoubleKeyFrame : DoubleKeyFrame
	{
		/// <summary>
		/// Initializes a new instance of the EasingDoubleKeyFrame class.
		/// </summary>
		public EasingDoubleKeyFrame()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the EasingDoubleKeyFrame class with the specified Double value.
		/// </summary>
		/// <param name="value">The initial Double value.</param>
		public EasingDoubleKeyFrame(double value)
			: base(value)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EasingDoubleKeyFrame class with the specified Double value and key time.
		/// </summary>
		/// <param name="value">The initial Double value.</param>
		/// <param name="keyTime">The initial key time.</param>
		public EasingDoubleKeyFrame(double value, KeyTime keyTime)
			: base(value, keyTime)
		{
		}

		/// <summary>
		/// Initializes a new instance of the EasingDoubleKeyFrame class with the specified Double value, key time, and easing function.
		/// </summary>
		/// <param name="value">The initial Double value.</param>
		/// <param name="keyTime">The initial key time.</param>
		/// <param name="easingFunction">The easing function.</param>
		public EasingDoubleKeyFrame(double value, KeyTime keyTime, EasingFunctionBase easingFunction)
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
			DependencyProperty.Register("EasingFunction", typeof(EasingFunctionBase), typeof(EasingDoubleKeyFrame), new FrameworkPropertyMetadata(null));

		internal override IEasingFunction GetEasingFunction()
		{
			return EasingFunction;
		}
	}
}
