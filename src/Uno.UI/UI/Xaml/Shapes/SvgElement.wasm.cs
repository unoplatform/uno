using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Wasm
{
	public partial class SvgElement : FrameworkElement
	{
		public SvgElement(string svgTag) : base(svgTag, isSvg: true)
		{
		}

		internal override bool IsViewHit() => true;
	}
}
