#nullable enable

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Uno.UI.Xaml.Controls.Extensions;

/// <summary>
/// Optionally implemented by an <see cref="IOverlayTextBoxViewExtension"/> whose platform has a system text
/// magnifier (iOS 17+: <c>UITextLoupeSession</c>), shown while the caret or a selection handle is dragged.
/// Points and rects are in <paramref name="xamlRoot"/> coordinates.
/// </summary>
internal interface INativeTextLoupeExtension
{
	/// <returns>False when the platform can't show a loupe.</returns>
	bool BeginLoupe(XamlRoot xamlRoot, Point point, Rect caretRect);

	void MoveLoupe(Point point, Rect caretRect);

	void EndLoupe();
}
