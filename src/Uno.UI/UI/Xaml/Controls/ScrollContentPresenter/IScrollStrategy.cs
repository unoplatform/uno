#if UNO_HAS_MANAGED_SCROLL_PRESENTER
#nullable enable

namespace Windows.UI.Xaml.Controls
{
	internal interface IScrollStrategy
	{
		void Initialize(ScrollContentPresenter presenter) { }
		void Update(UIElement view, double horizontalOffset, double verticalOffset, double zoom, bool disableAnimation);
	}
}
#endif
