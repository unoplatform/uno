using Microsoft.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// This interface defines common functionality between <see cref="GradientBrush"/> and <see cref="RadialGradientBrush"/>
	/// since, in UWP, <see cref="RadialGradientBrush"/> doesn't derive from <see cref="GradientBrush"/>
	/// </summary>
	internal partial interface IGradientBrush
	{
	}
}
