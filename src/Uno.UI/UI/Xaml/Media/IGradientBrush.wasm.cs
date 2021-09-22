using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	partial interface IGradientBrush
	{
		string ToCssString(Size size);
		UIElement ToSvgElement();
	}
}
