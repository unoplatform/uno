using CoreAnimation;
using CoreGraphics;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class GradientBrush : Brush
	{
		internal abstract CALayer GetLayer(CGSize size);
	}
}
