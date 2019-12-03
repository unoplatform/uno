using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Wasm
{
	public partial class SvgElement : FrameworkElement
	{
		private readonly Shape _parent;

		public SvgElement(string svgTag, Shape parent) : base(svgTag, isSvg: true)
		{
			_parent = parent;
		}

		internal override bool IsViewHit() => _parent?.Fill != null;
	}
}
