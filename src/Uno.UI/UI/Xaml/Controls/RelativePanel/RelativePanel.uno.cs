using Windows.Foundation;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls;

public partial class RelativePanel : Panel
{
	internal Size GetChildDesiredSize(View view) => GetElementDesiredSize(view);

	internal Size MeasureChild(View view, Size availableSize) => MeasureElement(view, availableSize);

	internal void ArrangeChild(View view, Rect finalRect) => ArrangeElement(view, finalRect);
}
