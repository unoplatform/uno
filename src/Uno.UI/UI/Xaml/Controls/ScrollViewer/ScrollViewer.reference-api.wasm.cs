using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollViewer
{
	protected override void OnPointerEntered(PointerRoutedEventArgs args) => base.OnPointerEntered(args);

	protected override void OnPointerMoved(PointerRoutedEventArgs args) => base.OnPointerMoved(args);

	protected override void OnPointerExited(PointerRoutedEventArgs args) => base.OnPointerExited(args);

	protected override void OnGotFocus(RoutedEventArgs args) => base.OnGotFocus(args);

	protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs args) => base.OnBringIntoViewRequested(args);
}
