using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationView : ContentControl
	{
		public event Foundation.TypedEventHandler<NavigationView, NavigationViewDisplayModeChangedEventArgs> DisplayModeChanged;

		public event Foundation.TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs> ItemInvoked;

		public event Foundation.TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> SelectionChanged;

		public event Foundation.TypedEventHandler<NavigationView, NavigationViewBackRequestedEventArgs> BackRequested;

		public event Foundation.TypedEventHandler<NavigationView, object> PaneClosed;

		public event Foundation.TypedEventHandler<NavigationView, NavigationViewPaneClosingEventArgs> PaneClosing;

		public event Foundation.TypedEventHandler<NavigationView, object> PaneOpened;

		public event Foundation.TypedEventHandler<NavigationView, object> PaneOpening;
	}
}
