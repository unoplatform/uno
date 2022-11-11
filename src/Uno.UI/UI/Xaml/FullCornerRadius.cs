using Windows.Foundation;

namespace Windows.UI.Xaml;

internal partial record struct FullCornerRadius
(
	Point TopLeft,
	Point TopRight,
	Point BottomRight,
	Point BottomLeft
);
