using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Wasm
{
	public partial class SvgElement : UIElement
	{
		public SvgElement(string svgTag) : base(svgTag, isSvg: true)
		{
		}
	}
}
