using Android.Animation;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class ExponentialEase
	{
		internal override ITimeInterpolator CreateTimeInterpolator()
		{
			return EasingFunctionHelpers.GetPowerTimeInterpolator((float)Exponent, EasingMode);
		}
	}
}
