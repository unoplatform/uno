using System.Collections.Generic;
using Windows.Foundation;
#if HAS_UNO_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
#else
using Windows.UI.Composition;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
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
