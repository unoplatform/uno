using System.Collections.Generic;
using Windows.Foundation;

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
