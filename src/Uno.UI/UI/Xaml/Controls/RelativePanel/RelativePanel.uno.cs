using Windows.Foundation;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls;

public partial class RelativePanel : Panel
{
	internal Size GetChildDesiredSize(View view) => GetElementDesiredSize(view);

	internal Size MeasureChild(View view, Size availableSize) => MeasureElement(view, availableSize);

	internal void ArrangeChild(View view, Rect finalRect) => ArrangeElement(view, finalRect);
}
