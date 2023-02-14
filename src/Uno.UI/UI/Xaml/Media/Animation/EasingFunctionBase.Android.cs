using System;
using Android.Animation;
using Android.Views.Animations;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public abstract partial class EasingFunctionBase
	{
		internal virtual ITimeInterpolator CreateTimeInterpolator()
		{
			return new LinearInterpolator();
		}
	}
}
