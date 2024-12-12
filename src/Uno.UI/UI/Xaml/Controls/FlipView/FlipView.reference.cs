using Windows.Foundation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls;

// The existence of these override is significant for Reference API.
// The implementation wouldn't matter at all, but they must exist to allow for a user class to inherit FlipView, and then override any of these methods, calling base in the implementation.
// If they don't exist in Reference API, calling base would call the wrong method, bypassing the override for Wasm/Skia.
// Note that the ReferenceImplComparer tool was updated to report these mistakes in https://github.com/unoplatform/uno/pull/14066

public partial class FlipView : Selector
{
	protected override void OnPointerWheelChanged(PointerRoutedEventArgs pArgs)
	{
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
	{
	}

	protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)
	{
	}

	protected override DependencyObject GetContainerForItemOverride() => null;

	protected override void OnItemsChanged(object e)
	{
	}

	protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
	{
	}

	protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
	{
	}

	protected override AutomationPeer OnCreateAutomationPeer() => null;

	protected override Size MeasureOverride(Size availableSize) => default;

	protected override Size ArrangeOverride(Size arrangeSize) => default;

	protected override void OnGotFocus(RoutedEventArgs pArgs)
	{
	}

	protected override void OnLostFocus(RoutedEventArgs pArgs)
	{
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
	{
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs pArgs)
	{
	}

	protected override void OnApplyTemplate()
	{
	}
}
