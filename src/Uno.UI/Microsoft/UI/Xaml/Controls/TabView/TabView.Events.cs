// MUX Reference: TabView.idl, commit 8aaf7f8

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabView
	{
		public event TypedEventHandler<TabView, object> AddTabButtonClick;

		public event SelectionChangedEventHandler SelectionChanged;

		public event TypedEventHandler<TabView, TabViewTabCloseRequestedEventArgs> TabCloseRequested;

		public event TypedEventHandler<TabView, TabViewTabDragCompletedEventArgs> TabDragCompleted;

		public event TypedEventHandler<TabView, TabViewTabDragStartingEventArgs> TabDragStarting;

		public event TypedEventHandler<TabView, TabViewTabDroppedOutsideEventArgs> TabDroppedOutside;

		public event TypedEventHandler<TabView, IVectorChangedEventArgs> TabItemsChanged;

		public event DragEventHandler TabStripDragOver;

		public event DragEventHandler TabStripDrop;
	}
}
