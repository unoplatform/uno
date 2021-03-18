using Windows.Foundation;
#if HAS_UNO_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		public event TypedEventHandler<NavigationView, NavigationViewDisplayModeChangedEventArgs> DisplayModeChanged;

		public event TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs> ItemInvoked;

		public event TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> SelectionChanged;

		public event TypedEventHandler<NavigationView, NavigationViewBackRequestedEventArgs> BackRequested;

		public event TypedEventHandler<NavigationView, object> PaneClosed;

		public event TypedEventHandler<NavigationView, NavigationViewPaneClosingEventArgs> PaneClosing;

		public event TypedEventHandler<NavigationView, object> PaneOpened;

		public event TypedEventHandler<NavigationView, object> PaneOpening;
	}
}
