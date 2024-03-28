namespace Windows.UI.Xaml.Media.Animation
{
	public partial class CubicEase : EasingFunctionBase
	{
		private protected override double EaseInCore(double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime;
		}
	}
}
