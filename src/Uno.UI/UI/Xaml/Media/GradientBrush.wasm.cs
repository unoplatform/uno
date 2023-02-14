using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media
{
	partial class GradientBrush
	{
		internal abstract string ToCssString(Size size);
		internal abstract UIElement ToSvgElement();
	}
}
