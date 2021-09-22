using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	partial class GradientBrush
	{
		string IGradientBrush.ToCssString(Size size) => ToCssString(size);
		UIElement IGradientBrush.ToSvgElement() => ToSvgElement();

		internal abstract string ToCssString(Size size);
		internal abstract UIElement ToSvgElement();
	}
}
