using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class TabView
	{
		internal const double c_tabShadowDepth = 16.0;
		internal const string c_tabViewShadowDepthName = "TabViewShadowDepth";

		private bool m_updateTabWidthOnPointerLeave = false;
		private bool m_pointerInTabstrip = false;

		private ColumnDefinition m_leftContentColumn;
		private ColumnDefinition m_tabColumn;
		private ColumnDefinition m_addButtonColumn;
		private ColumnDefinition m_rightContentColumn;

		private ListView m_listView;
		private ContentPresenter m_tabContentPresenter;
		private ContentPresenter m_rightContentPresenter;
		private Grid m_tabContainerGrid;
		private ScrollViewer m_scrollViewer;
		private RepeatButton m_scrollDecreaseButton;
		private RepeatButton m_scrollIncreaseButton;
		private Button m_addButton;
		private ItemsPresenter m_itemsPresenter;

		private Grid m_shadowReceiver;

		private readonly SerialDisposable m_listViewLoadedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_tabStripPointerExitedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_tabStripPointerEnteredRevoker = new SerialDisposable();
		private readonly SerialDisposable m_listViewSelectionChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_listViewGettingFocusRevoker = new SerialDisposable();

		private readonly SerialDisposable m_listViewCanReorderItemsPropertyChangedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_listViewAllowDropPropertyChangedRevoker = new SerialDisposable();

		private readonly SerialDisposable m_listViewDragItemsStartingRevoker = new SerialDisposable();
		private readonly SerialDisposable m_listViewDragItemsCompletedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_listViewDragOverRevoker = new SerialDisposable();
		private readonly SerialDisposable m_listViewDropRevoker = new SerialDisposable();

		private readonly SerialDisposable m_scrollViewerLoadedRevoker = new SerialDisposable();
		private readonly SerialDisposable m_scrollViewerViewChangedRevoker = new SerialDisposable();

		private readonly SerialDisposable m_addButtonClickRevoker = new SerialDisposable();

		private readonly SerialDisposable m_scrollDecreaseClickRevoker = new SerialDisposable();
		private readonly SerialDisposable m_scrollIncreaseClickRevoker = new SerialDisposable();

		private readonly SerialDisposable m_itemsPresenterSizeChangedRevoker = new SerialDisposable();

		private DispatcherHelper m_dispatcherHelper;

		private string m_tabCloseButtonTooltipText;

		private Size previousAvailableSize;

#if HAS_UNO
		//TODO Uno specific: Watches scrollable width to update visibility of scroll buttons.
		private readonly SerialDisposable m_ScrollViewerScrollableWidthPropertyChangedRevoker = new SerialDisposable();
#endif
	}
}
