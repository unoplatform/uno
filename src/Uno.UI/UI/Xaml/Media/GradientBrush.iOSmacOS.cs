using CoreAnimation;
using CoreGraphics;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class GradientBrush : Brush
	{
		CALayer IGradientBrush.GetLayer(CGSize size) => GetLayer(size);

		internal abstract CALayer GetLayer(CGSize size);
	}
}
