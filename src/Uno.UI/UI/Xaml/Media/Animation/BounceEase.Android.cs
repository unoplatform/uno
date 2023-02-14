using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;
using Android.Views.Animations;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class BounceEase
	{
		internal override ITimeInterpolator CreateTimeInterpolator()
		{
			//There are no EaseIn or EaseOut For Bounce Interpolator in Android so we use the simple BounceInterpolator
			return new BounceInterpolator();
		}
	}
}
