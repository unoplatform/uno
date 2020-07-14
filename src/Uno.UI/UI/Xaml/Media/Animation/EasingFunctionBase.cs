using Uno.Extensions;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
#if XAMARIN_ANDROID
using Android.Animation;
#endif

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class EasingFunctionBase : DependencyObject, IEasingFunction
	{
		public EasingFunctionBase()
		{
			InitializeBinder();
		}

		public EasingMode EasingMode
		{
			get => (EasingMode)this.GetValue(EasingModeProperty);
			set => this.SetValue(EasingModeProperty, value);
		}
		
		public static DependencyProperty EasingModeProperty { get ; } =
			DependencyProperty.Register("EasingMode", typeof(EasingMode), typeof(EasingFunctionBase), new FrameworkPropertyMetadata(EasingMode.EaseOut));

		public virtual double Ease(double currentTime, double startValue, double finalValue, double duration) =>
			// Return linear interpolation instead of an exception for unimplemented easing functions.
			currentTime == 0 ? startValue : Lerp(startValue, finalValue, currentTime / duration);

		private static double Lerp(double first, double last, double by) => first * (1.0 - @by) + last * @by;
	}
}
