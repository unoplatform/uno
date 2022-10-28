using Windows.Foundation;

namespace Windows.UI.Xaml;

internal record struct FullCornerRadius
(
	Point TopLeft,
	Point TopRight,
	Point BottomRight,
	Point BottomLeft
);
