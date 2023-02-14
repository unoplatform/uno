using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;
using Android.Views.Animations;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class BackEase
	{
		internal override ITimeInterpolator CreateTimeInterpolator()
		{
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					return new AnticipateInterpolator((float)this.Amplitude);
				case EasingMode.EaseOut:
					return new OvershootInterpolator((float)this.Amplitude);
				case EasingMode.EaseInOut:
					return new AnticipateOvershootInterpolator((float)this.Amplitude);
				default:
					throw new NotSupportedException("This easing mode is not supported");
			}
		}
	}
}
