using CoreAnimation;
using CoreGraphics;

namespace Microsoft.UI.Xaml.Media
{
	public abstract partial class GradientBrush : Brush
	{
		internal abstract CALayer GetLayer(CGSize size);
	}
}
