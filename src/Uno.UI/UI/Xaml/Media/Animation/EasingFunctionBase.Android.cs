using Android.Animation;
using Android.Views.Animations;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class EasingFunctionBase
	{
		private sealed class AndroidTimeInterpolator : BaseInterpolator
		{
			private readonly EasingFunctionBase _easingFunctionBase;

			public AndroidTimeInterpolator(EasingFunctionBase easingFunctionBase)
				=> _easingFunctionBase = easingFunctionBase;

			public override float GetInterpolation(float input) => (float)_easingFunctionBase.Ease(input);
		}

		internal ITimeInterpolator CreateTimeInterpolator()
		{
			return new AndroidTimeInterpolator(this);
		}
	}
}
