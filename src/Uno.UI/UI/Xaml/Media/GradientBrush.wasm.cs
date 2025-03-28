using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	partial class GradientBrush
	{
		internal abstract string ToCssString(Size size);
		internal abstract UIElement ToSvgElement();
	}
}
