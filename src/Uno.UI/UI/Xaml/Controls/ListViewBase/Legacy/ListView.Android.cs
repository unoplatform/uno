using Android.Views;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using Windows.UI.Xaml.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Collections;
using Android.Widget;
using Android.Runtime;
using Uno.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Controls.Legacy
{
	public partial class ListView : Android.Widget.ListView, DependencyObject, IListView
	{
		private SerialDisposable _clickRegistration = new SerialDisposable();
		private ICommand _itemLongClickCommand;
		private readonly AbsListViewSecondaryPool _secondaryPool;
		private ListViewAdapter _adapter;

		public const int DefaultCustomViewTypeCount = 50;
		private int _customViewTypeCount = DefaultCustomViewTypeCount;

		public event SelectionChangedEventHandler SelectionChanged;

		/// <summary>
		/// This is used by Android's view recycling. The default value should normally be sufficient. Set it higher if a larger number of unique DataTemplates is needed.
		/// </summary>
		public int CustomViewTypeCount
		{
			get { return _customViewTypeCount; }
			set
			{
				_customViewTypeCount = value;
				if (ListViewAdapter != null)
				{
					ListViewAdapter.CustomViewTypeCount = _customViewTypeCount;

					//Forces refresh of ViewTypeCount, because it is only checked when Adapter is set (see http://developer.android.com/reference/android/widget/BaseAdapter.html#getViewTypeCount() )
					this.Adapter = this.Adapter;
				}
			}
		}

		public ListView()
			: base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			NativeInstanceHelper.CreateNativeInstance(base.GetType(), this, ContextHelper.Current, base.SetHandle);

			InitializeBinder();
			IFrameworkElementHelper.Initialize(this);

			SetClipToPadding(false);
			ScrollBarStyle = ScrollbarStyles.OutsideOverlay; // prevents padding from affecting scrollbar position

			_secondaryPool = new AbsListViewSecondaryPool(TryGetItemViewTypeFromItem, _customViewTypeCount);
			SetRecyclerListener(_secondaryPool);
		}

		public new event ItemClickEventHandler ItemClick;

		partial void OnLoadedPartial()
		{
			SetupItemClickListener();

			if (_adapter != null)
			{
				// The adapter has already been initialized.
				// Re-initializing the adapter would cause the ListView to lose its state (i.e., scroll position) after a reload.
				return;
			}

			_adapter = new ListViewAdapter();
			_adapter.ItemContainerStyle = ItemContainerStyle;
			_adapter.ItemContainerFactory = () => new ListViewItem() { ShouldHandlePressed = false };
			_adapter.ItemTemplate = ItemTemplate;
			_adapter.ItemTemplateSelector = ItemTemplateSelector;
			_adapter.ItemClickCommand = _itemClickCommand;
			_adapter.Header = Header;
			_adapter.Footer = Footer;
			_adapter.HeaderTemplate = HeaderTemplate;
			_adapter.FooterTemplate = FooterTemplate;
			_adapter.ItemsSource = ItemsSource;
			_adapter.CustomViewTypeCount = this.CustomViewTypeCount;
			_adapter.GroupStyle = this.GroupStyle;
			_adapter.ItemContainerHolderStretchOrientation = Windows.UI.Xaml.Controls.Orientation.Horizontal;
			_adapter.SecondaryPool = _secondaryPool;

			SelectedItems.Safe()
				.ForEach((object i) => _adapter.SetItemSelection(i, null, true));

			Adapter = _adapter;

			IsResetScrollOnItemsSourceChanged = true;
		}

		partial void OnUnloadedPartial()
		{
			_clickRegistration.Disposable = null;
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var heightMode = ViewHelper.MeasureSpecGetMode(heightMeasureSpec);

			if (heightMode == MeasureSpecMode.Unspecified)
			{
				// If the list view is measured with an Unspecified height
				// the returned size is the height of the first item of the collection.
				// For the ListView to measure all its children, it needs to be measured with 
				// an AtMost mode.
				heightMeasureSpec = ViewHelper.MakeMeasureSpec(int.MaxValue, MeasureSpecMode.AtMost);
			}

			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		public ICommand Command
		{
			get { return ItemClickCommand; }
			set { ItemClickCommand = value; }
		}

		#region ItemTemplate Dependency Property
		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)this.GetValue(ItemTemplateProperty); }
			set { this.SetValue(ItemTemplateProperty, value); }
		}

		public static DependencyProperty ItemTemplateProperty { get; } =
			DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(ListView), new FrameworkPropertyMetadata(defaultValue: default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, propertyChangedCallback: (d, s) => (d as ListView)?.OnItemTemplateChanged()));

		private void OnItemTemplateChanged()
		{
			if (ListViewAdapter != null)
			{
				ListViewAdapter.ItemTemplate = ItemTemplate;
			}
		}
		#endregion

		#region HeaderTemplate Dependency Property
		public DataTemplate HeaderTemplate
		{
			get { return (DataTemplate)this.GetValue(HeaderTemplateProperty); }
			set { this.SetValue(HeaderTemplateProperty, value); }
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(ListView), new FrameworkPropertyMetadata(defaultValue: default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, propertyChangedCallback: (d, s) => (d as ListView)?.OnHeaderTemplateChanged()));

		private void OnHeaderTemplateChanged()
		{
			if (ListViewAdapter != null)
			{
				ListViewAdapter.HeaderTemplate = HeaderTemplate;
			}
		}
		#endregion

		#region FooterTemplate Dependency Property
		public DataTemplate FooterTemplate
		{
			get { return (DataTemplate)this.GetValue(FooterTemplateProperty); }
			set { this.SetValue(FooterTemplateProperty, value); }
		}

		public static DependencyProperty FooterTemplateProperty { get; } =
			DependencyProperty.Register("FooterTemplate", typeof(DataTemplate), typeof(ListView), new FrameworkPropertyMetadata(defaultValue: default(DataTemplate), options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, propertyChangedCallback: (d, s) => (d as ListView)?.OnFooterTemplateChanged()));

		private void OnFooterTemplateChanged()
		{
			if (ListViewAdapter != null)
			{
				ListViewAdapter.FooterTemplate = FooterTemplate;
			}
		}
		#endregion

		#region ItemTemplateSelector Dependency Property
		public DataTemplateSelector ItemTemplateSelector
		{
			get { return (DataTemplateSelector)this.GetValue(ItemTemplateSelectorProperty); }
			set { this.SetValue(ItemTemplateSelectorProperty, value); }
		}

		public static DependencyProperty ItemTemplateSelectorProperty { get; } =
			DependencyProperty.Register("ItemTemplateSelector", typeof(DataTemplateSelector), typeof(ListView), new FrameworkPropertyMetadata(defaultValue: default(DataTemplateSelector), propertyChangedCallback: (d, s) => (d as ListView)?.OnItemTemplateSelectorChanged()));

		private void OnItemTemplateSelectorChanged()
		{
			if (ListViewAdapter != null)
			{
				ListViewAdapter.ItemTemplateSelector = ItemTemplateSelector;
			}
		}
		#endregion

		public ItemsPanelTemplate ItemsPanel { get; set; }

		public Thickness Padding
		{
			get
			{
				return new Thickness
				{
					Left = ViewHelper.PhysicalToLogicalPixels(PaddingLeft),
					Top = ViewHelper.PhysicalToLogicalPixels(PaddingTop),
					Right = ViewHelper.PhysicalToLogicalPixels(PaddingRight),
					Bottom = ViewHelper.PhysicalToLogicalPixels(PaddingBottom),
				};
			}

			set
			{
				var l = (int)ViewHelper.LogicalToPhysicalPixels(value.Left);
				var t = (int)ViewHelper.LogicalToPhysicalPixels(value.Top);
				var r = (int)ViewHelper.LogicalToPhysicalPixels(value.Right);
				var b = (int)ViewHelper.LogicalToPhysicalPixels(value.Bottom);
				SetPadding(l, t, r, b);
			}
		}

		#region Header Dependency Property
		public object Header
		{
			get { return (object)this.GetValue(HeaderProperty); }
			set { this.SetValue(HeaderProperty, value); }
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register("Header", typeof(object), typeof(ListView), new FrameworkPropertyMetadata(defaultValue: default(object), propertyChangedCallback: (d, s) => (d as ListView)?.OnHeaderChanged()));

		private void OnHeaderChanged()
		{
			if (ListViewAdapter != null)
			{
				ListViewAdapter.Header = Header;
			}
		}
		#endregion

		#region Footer Dependency Property
		public object Footer
		{
			get { return (object)this.GetValue(FooterProperty); }
			set { this.SetValue(FooterProperty, value); }
		}

		public static DependencyProperty FooterProperty { get; } =
			DependencyProperty.Register("Footer", typeof(object), typeof(ListView), new FrameworkPropertyMetadata(defaultValue: default(object), propertyChangedCallback: (d, s) => (d as ListView)?.OnFooterChanged()));

		private void OnFooterChanged()
		{
			if (ListViewAdapter != null)
			{
				ListViewAdapter.Footer = Footer;
			}
		}
		#endregion

		public object HeaderDataContext
		{
			get { return (Header as IDataContextProvider).SelectOrDefault(d => d.DataContext); }
			set
			{
				var provider = Header as IDataContextProvider;

				if (provider != null)
				{
					provider.DataContext = value;
				}
			}
		}

		public object FooterDataContext
		{
			get { return (Footer as IDataContextProvider).SelectOrDefault(d => d.DataContext); }
			set
			{
				var provider = Footer as IDataContextProvider;

				if (provider != null)
				{
					provider.DataContext = value;
				}
			}
		}

		public bool IsResetScrollOnItemsSourceChanged { get; set; }

		#region ItemsSource Dependency Property
		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register("ItemsSource", typeof(object), typeof(ListView), new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

		public object ItemsSource
		{
			get { return (object)this.GetValue(ItemsSourceProperty); }
			set { this.SetValue(ItemsSourceProperty, value); }
		}
		#endregion

		#region SelectionMode Dependency Property

		public static DependencyProperty SelectionModeProperty { get; } =
			DependencyProperty.Register(
				"SelectionMode",
				typeof(ListViewSelectionMode),
				typeof(ListView),
				new FrameworkPropertyMetadata(defaultValue: ListViewSelectionMode.Single, propertyChangedCallback: OnSelectionModeChanged)
			);
		public ListViewSelectionMode SelectionMode
		{
			get { return (ListViewSelectionMode)this.GetValue(SelectionModeProperty); }
			set { this.SetValue(SelectionModeProperty, value); }
		}
		#endregion

		#region SelectedItems Dependency Property

		public IList<object> SelectedItems
		{
			get { return (IList<object>)this.GetValue(SelectedItemsProperty); }
			set { this.SetValue(SelectedItemsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
		public static DependencyProperty SelectedItemsProperty { get; } =
			DependencyProperty.Register(
				"SelectedItems",
				typeof(IList<object>),
				typeof(ListView),
				new FrameworkPropertyMetadata(
					defaultValue: new List<object>(),
					propertyChangedCallback: OnSelectedItemsChanged
				)
			);
		#endregion

		#region SelectedItem Dependency Property

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(
				"SelectedItem",
				typeof(object),
				typeof(ListView),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => (s as ListView).OnSelectedItemChanged(e.OldValue, e.NewValue)
				)
			);

		public new object SelectedItem
		{
			get { return (object)this.GetValue(SelectedItemProperty); }
			set { this.SetValue(SelectedItemProperty, value); }
		}
		#endregion

		#region ItemContainerStyle Dependency Property

		public static DependencyProperty ItemContainerStyleProperty { get; } =
			DependencyProperty.Register(
				"ItemContainerStyle",
				typeof(Style),
				typeof(ListView),
				new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, propertyChangedCallback: OnItemContainerStyleChanged)
			);

		public Style ItemContainerStyle
		{
			get { return (Style)this.GetValue(ItemContainerStyleProperty); }
			set { this.SetValue(ItemContainerStyleProperty, value); }
		}

		#endregion

		#region GroupStyle Dependency Property

		public static DependencyProperty GroupStyleProperty { get; } =
			DependencyProperty.Register(
				"GroupStyle",
				typeof(GroupStyle),
				typeof(ListView),
				new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnGroupStyleChanged)
			);

		internal void RegisterRecycledAction(View convertView, Action a)
		{

		}

		public GroupStyle GroupStyle
		{
			get { return (GroupStyle)this.GetValue(GroupStyleProperty); }
			set { this.SetValue(GroupStyleProperty, value); }
		}

		#endregion

		#region IsItemClickEnabled Dependency Property

		public bool IsItemClickEnabled
		{
			get { return (bool)GetValue(IsItemClickEnabledProperty); }
			set { SetValue(IsItemClickEnabledProperty, value); }
		}

		public static DependencyProperty IsItemClickEnabledProperty { get; } =
			DependencyProperty.Register("IsItemClickEnabled", typeof(bool), typeof(ListView), new FrameworkPropertyMetadata(false));
		#endregion

		#region UnselectOnClick Dependency Property
		/// <summary>
		/// Offers the possibility to unselect the currently selected item when the SelectionMode is Single.
		/// Clicking on the currently selected item will unselect it.
		/// </summary>
		public bool UnselectOnClick
		{
			get { return (bool)GetValue(UnselectOnClickProperty); }
			set { SetValue(UnselectOnClickProperty, value); }
		}

		public static DependencyProperty UnselectOnClickProperty { get; } =
			DependencyProperty.Register("UnselectOnClick", typeof(bool), typeof(ListView), new FrameworkPropertyMetadata(default(bool)));
		#endregion

		private static void OnGroupStyleChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var listView = dependencyObject as ListView;

			if (listView != null && listView.ListViewAdapter != null)
			{
				listView.ListViewAdapter.GroupStyle = args.NewValue as GroupStyle;
			}
		}

		private static void OnItemContainerStyleChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var listView = dependencyObject as ListView;

			if (listView != null && listView.ListViewAdapter != null)
			{
				listView.ListViewAdapter.ItemContainerStyle = args.NewValue as Style;
			}
		}

		private static void OnItemsSourceChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			ListView list = d as ListView;

			if (list != null)
			{
				//We stop the execution here if the list is collapsed since we do not want to call a RequestLayout for no reason.
				if (list.Visibility == Visibility.Collapsed)
				{
					return;
				}

				list.SetListViewAdapterItemsSource();
			}
		}

		partial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue)
		{
			if (newValue == Visibility.Visible)
			{
				SetListViewAdapterItemsSource();
			}
		}

		public void SetListViewAdapterItemsSource()
		{
			if (ListViewAdapter != null)
			{
				if (IsResetScrollOnItemsSourceChanged)
				{
					_ = Dispatcher.RunAsync(
						Windows.UI.Core.CoreDispatcherPriority.Normal,
						() =>
					{
						SetSelection(0);
						RequestLayout();
					});
				}
				ListViewAdapter.ItemsSource = ItemsSource;
			}
			else
			{
				RequestLayout();
			}
		}

		private static void OnSelectionModeChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var listView = dependencyObject as ListView;

			if (listView != null)
			{
				var newMode = (ListViewSelectionMode)args.NewValue;
				var oldMode = (ListViewSelectionMode)args.OldValue;

				if (newMode == ListViewSelectionMode.None)
				{
					listView.SelectedItems = Array.Empty<object>();
				}
				else if (newMode == ListViewSelectionMode.Single && oldMode == ListViewSelectionMode.Multiple)
				{
					var firstSelection = listView.SelectedItems.FirstOrDefault();

					listView.SelectedItems = firstSelection.SelectOrDefault(s => new[] { s }, Array.Empty<object>());
				}
			}
		}

		private static void OnSelectedItemsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var listView = dependencyObject as ListView;

			if (listView != null)
			{
				var newSelection = (IList<object>)args.NewValue;

				listView.UpdateSelection(newSelection);
			}
		}

		private void OnSelectedItemChanged(object oldSelectedItem, object selectedItem)
		{
			UpdateSelection(new[] { selectedItem });
		}

		private void UpdateSelection(IList<object> newSelection)
		{
			if (ListViewAdapter != null)
			{
				if (!ListViewAdapter.SelectedItems.SequenceEqual(newSelection))
				{
					var removed = ListViewAdapter
							.SelectedItems
							.Except(newSelection)
							.ToArray();

					var added = newSelection
					.Except(ListViewAdapter.SelectedItems)
						.ToArray();

					removed.ForEach((object i) => ListViewAdapter.SetItemSelection(i, null, false));
					added.ForEach((object i) => ListViewAdapter.SetItemSelection(i, null, true));

					SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, removed, added));
				}
			}
		}

		protected ListViewAdapter ListViewAdapter
		{
			get { return Adapter as ListViewAdapter; }
		}

		private ICommand _itemClickCommand;
		public ICommand ItemClickCommand
		{
			get
			{
				return _itemClickCommand;
			}
			set
			{
				_itemClickCommand = value;

				if (ListViewAdapter != null)
				{
					ListViewAdapter.ItemClickCommand = value;
				}
			}
		}

		protected virtual void SetupItemClickListener()
		{
			_clickRegistration.Disposable = Disposable.Create(() => base.ItemClick -= OnItemClick);

			base.ItemClick += OnItemClick;
		}

		private void OnItemClick(object sender, ItemClickEventArgs args)
		{
			if (IsItemClickEnabled)
			{
				ExecuteCommandOnItem(ItemClickCommand, args.Position);
			}

			ItemClick?.Invoke(this, new Windows.UI.Xaml.Controls.ItemClickEventArgs { ClickedItem = ListViewAdapter.GetItemAt(args.Position) });

			HandleItemSelection(args);
		}

		private void HandleItemSelection(ItemClickEventArgs args)
		{
			if (SelectionMode != ListViewSelectionMode.None)
			{
				var newSelection = ListViewAdapter.GetItemAt(args.Position);

				switch (SelectionMode)
				{
					case ListViewSelectionMode.Single:
						var selectedItem = ListViewAdapter.SelectedItems.FirstOrDefault();

						// Unselect the current item only if a new selection is made or 
						// the option to unselect the current item is activated.
						if (selectedItem != null && (selectedItem != newSelection || UnselectOnClick))
						{
							ListViewAdapter.SetItemSelection(selectedItem, null, false);
						}

						if (selectedItem != newSelection)
						{
							ListViewAdapter.SetItemSelection(
								newSelection,
								(args.View as ItemContainerHolder)?.Child as SelectorItem,
								true
							);
						}

						SelectedItem = newSelection;

						if (selectedItem != newSelection)
						{
							SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, new[] { selectedItem }, new[] { newSelection }));
						}
						break;

					case ListViewSelectionMode.Multiple:
						ListViewAdapter.SetItemSelection(
							ListViewAdapter.GetItemAt(args.Position),
							args.View as SelectorItem,
							!ListViewAdapter.SelectedItems.Contains(newSelection)
						);
						break;
				}

				SelectedItems = ListViewAdapter.SelectedItems.ToArray();

			}
		}

		public ICommand ItemLongClickCommand
		{
			get
			{
				return _itemLongClickCommand;
			}

			set
			{
				_itemLongClickCommand = value;
				SetupItemLongClickListener();
			}
		}

		protected virtual void SetupItemLongClickListener()
		{
			if (ItemLongClickCommand != null)
			{
				base.ItemLongClick += ExecuteLongClickCommandEvent(ItemLongClickCommand);
			}
			else
			{
				base.ItemLongClick -= ExecuteLongClickCommandEvent(ItemLongClickCommand);
			}
		}

		private EventHandler<ItemLongClickEventArgs> ExecuteLongClickCommandEvent(ICommand command)
		{
			return (sender, args) => ExecuteCommandOnItem(command, args.Position);
		}

		protected void ExecuteCommandOnItem(ICommand command, int position)
		{
			if (command == null)
				return;

			var item = ListViewAdapter.GetItemAt(position);
			if (item == null)
				return;

			if (!command.CanExecute(item))
				return;

			command.Execute(item);
		}

		private int TryGetItemViewTypeFromItem(int position)
		{
			return _adapter?.GetItemViewType(position) ?? 0;
		}

		protected override void RemoveDetachedView(View child, bool animate)
		{
			base.RemoveDetachedView(child, animate);

			_secondaryPool.RemoveFromRecycler(child);
		}

		/// <summary>
		/// Scrolls the list to bring the specified data item into view.
		/// </summary>
		/// <param name="item">The data item to bring into view.</param>
		public void ScrollIntoView(object item)
		{
			var index = IndexForItem(item);

			if (index != -1)
			{
				ScrollIntoViewInner(index);
			}

		}

		private void ScrollIntoViewInner(int index)
		{
			//SmoothScrollToPosition is using the position and not the index
			SmoothScrollToPosition(index + 1);
		}

		private int IndexForItem(object item)
		{
			return (ItemsSource as IEnumerable)?.IndexOf(item) ?? -1;
		}
	}
}
