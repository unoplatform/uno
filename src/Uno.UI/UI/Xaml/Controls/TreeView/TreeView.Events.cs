using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeView
	{
		public event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> Collapsed;

		public event TypedEventHandler<TreeView, TreeViewDragItemsCompletedEventArgs> DragItemsCompleted;

		public event TypedEventHandler<TreeView, TreeViewDragItemsStartingEventArgs> DragItemsStarting;

		public event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> Expanding;

		public event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> ItemInvoked;

		private event TypedEventHandler<TreeView, ContainerContentChangingEventArgs> ContainerContentChanged;
	}
}
