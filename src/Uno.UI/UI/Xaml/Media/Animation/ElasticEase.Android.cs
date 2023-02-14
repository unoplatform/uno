using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;
using Android.Views.Animations;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ElasticEase
	{
		internal override ITimeInterpolator CreateTimeInterpolator()
		{
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					return new OvershootInterpolator();
				case EasingMode.EaseOut:
					return new AnticipateInterpolator();
				case EasingMode.EaseInOut:
					return new AnticipateOvershootInterpolator();
				default:
					throw new NotSupportedException("This easing mode is not supported.");
			}
		}
	}
}
