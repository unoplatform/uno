using CoreAnimation;
using CoreGraphics;

namespace Windows.UI.Xaml.Media
{
    partial interface IGradientBrush
	{
        CALayer GetLayer(CGSize size);
    }
}
