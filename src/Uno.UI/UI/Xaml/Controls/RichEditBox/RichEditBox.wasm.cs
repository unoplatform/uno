#nullable enable

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RichEditBox
	{
		protected override void OnPointerEntered(PointerRoutedEventArgs e) => base.OnPointerEntered(e);

		protected override void OnPointerExited(PointerRoutedEventArgs e) => base.OnPointerExited(e);

		protected override void OnPointerPressed(PointerRoutedEventArgs e) => base.OnPointerPressed(e);

		protected override void OnPointerMoved(PointerRoutedEventArgs e) => base.OnPointerMoved(e);

		protected override void OnPointerReleased(PointerRoutedEventArgs e) => base.OnPointerReleased(e);

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs e) => base.OnPointerCaptureLost(e);

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e) => base.OnDoubleTapped(e);

		protected override void OnRightTapped(RightTappedRoutedEventArgs e) => base.OnRightTapped(e);

		protected override void OnApplyTemplate() => base.OnApplyTemplate();

		protected override void OnGotFocus(RoutedEventArgs e) => base.OnGotFocus(e);

		protected override void OnLostFocus(RoutedEventArgs e) => base.OnLostFocus(e);

		protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs e) => base.OnBringIntoViewRequested(e);

		protected override void OnFontSizeChanged(double oldValue, double newValue) => base.OnFontSizeChanged(oldValue, newValue);

		protected override void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue) => base.OnFontFamilyChanged(oldValue, newValue);

		protected override void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue) => base.OnFontStyleChanged(oldValue, newValue);

		protected override void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue) => base.OnFontWeightChanged(oldValue, newValue);
	}
}