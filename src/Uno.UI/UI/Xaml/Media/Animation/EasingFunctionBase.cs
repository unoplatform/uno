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
			get { return (EasingMode)this.GetValue(EasingModeProperty); }
			set { this.SetValue(EasingModeProperty, value); }
		}
		
		public static readonly DependencyProperty EasingModeProperty =
			DependencyProperty.Register("EasingMode", typeof(EasingMode), typeof(EasingFunctionBase), new PropertyMetadata(EasingMode.EaseOut));

		public virtual double Ease(double currentTime, double startValue, double finalValue, double duration) { throw new NotSupportedException(); }
	}
}
