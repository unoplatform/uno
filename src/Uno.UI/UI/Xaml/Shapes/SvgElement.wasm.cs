using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Wasm
{
	public partial class SvgElement : UIElement
	{
		public SvgElement(string svgTag) : base(svgTag, isSvg: true)
		{
		}
	}
}
