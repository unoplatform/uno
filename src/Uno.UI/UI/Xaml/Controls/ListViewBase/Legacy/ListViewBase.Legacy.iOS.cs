using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Views.Controls;
using System.Collections.Specialized;
using Uno.Disposables;
using Windows.UI.Core;
using Uno.UI.Controls;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;

using Uno.Foundation.Logging;

using Foundation;
using UIKit;
using CoreGraphics;
using CoreAnimation;

namespace Uno.UI.Controls.Legacy
{
	public enum ScrollIntoViewAlignment
	{
		Default,
		Leading,
	}

	public abstract partial class ListViewBase : BindableUICollectionView, IFrameworkElement, ISelector
	{
		#region Constants
		public static readonly NSString ListViewItemReuseIdentifierNS = new NSString(nameof(ListViewItemReuseIdentifier));
		public const string ListViewItemReuseIdentifier = nameof(ListViewItemReuseIdentifier);

		public static readonly NSString ListViewItemElementKindNS = new NSString(nameof(ListViewItemElementKind));
		public const string ListViewItemElementKind = nameof(ListViewItemElementKind);

		public static readonly NSString ListViewHeaderReuseIdentifierNS = new NSString(nameof(ListViewHeaderReuseIdentifier));
		public const string ListViewHeaderReuseIdentifier = nameof(ListViewHeaderReuseIdentifier);

		public static readonly NSString ListViewHeaderElementKindNS = new NSString(nameof(ListViewHeaderElementKind));
		public const string ListViewHeaderElementKind = nameof(ListViewHeaderElementKind);

		public static readonly NSString ListViewFooterReuseIdentifierNS = new NSString(nameof(ListViewFooterReuseIdentifier));
		public const string ListViewFooterReuseIdentifier = nameof(ListViewFooterReuseIdentifier);

		public static readonly NSString ListViewFooterElementKindNS = new NSString(nameof(ListViewFooterElementKind));
		public const string ListViewFooterElementKind = nameof(ListViewFooterElementKind);

		public static readonly NSString ListViewSectionHeaderReuseIdentifierNS = new NSString(nameof(ListViewSectionHeaderReuseIdentifier));
		public const string ListViewSectionHeaderReuseIdentifier = nameof(ListViewSectionHeaderReuseIdentifier);

		public static readonly NSString ListViewSectionHeaderElementKindNS = new NSString(nameof(ListViewSectionHeaderElementKind));
		public const string ListViewSectionHeaderElementKind = nameof(ListViewSectionHeaderElementKind);

		/// <summary>
		/// This property enables animations when using the ScrollIntoView Method.
		/// </summary>
		public bool AnimateScrollIntoView { get; set; } = Uno.UI.FeatureConfiguration.ListViewBase.AnimateScrollIntoView;
		#endregion

		#region Members
		private readonly SerialDisposable _notifyCollectionChanged = new SerialDisposable();
		private readonly SerialDisposable _notifyCollectionGroupsChanged = new SerialDisposable();
		private DataTemplateSelector _itemTemplateSelector;
		private ICommand _itemClickCommand;
		private DataTemplate _itemTemplate;
		private bool _needsReloadData;
		/// <summary>
		/// ReloadData() has been called, but the layout hasn't been updated. During this window, in-place modifications to the
		/// collection (InsertItems, etc) shouldn't be called because they will result in a NSInternalInconsistencyException
		/// </summary>
		private bool _needsLayoutAfterReloadData;
		private bool _lastCollectionChangedActionWasReset;
		#endregion

		#region Properties
		new internal ListViewBaseSource Source
		{
			get { return base.Source as ListViewBaseSource; }
			set
			{
				var oldSource = Source;
				base.Source = value;
				OnSourceChanged(oldSource, value);
			}
		}

		/// <summary>
		/// Deprecated, use ItemClickCommand instead.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public ICommand Command
		{
			get { return ItemClickCommand; }
			set
			{
				ItemClickCommand = value;
			}
		}

		public ICommand ItemClickCommand
		{
			get { return _itemClickCommand; }
			set
			{
				_itemClickCommand = value;
				if (Source != null)
				{
					Source.ItemClickCommand = _itemClickCommand;
				}
			}
		}

		public event ItemClickEventHandler ItemClick;

		public bool IsItemClickEnabled { get; set; } = true;

		public Style ItemContainerStyle
		{
			get;
			set;
		}

		public StyleSelector ItemContainerStyleSelector
		{
			get;
			set;
		}

		public virtual DataTemplate ItemTemplate
		{
			get { return _itemTemplate; }
			set
			{
				_itemTemplate = value;
				SetNeedsReloadData();
			}
		}

		public DataTemplate HeaderTemplate { get; set; }

		public DataTemplate FooterTemplate { get; set; }

		public DataTemplateSelector ItemTemplateSelector
		{
			get { return _itemTemplateSelector; }
			set
			{
				_itemTemplateSelector = value;
				SetNeedsReloadData();
			}
		}

		public event SelectionChangedEventHandler SelectionChanged;

		public bool NeedsReloadData => _needsReloadData;
		#endregion

		//TODO: this should be IObservableVector<GroupStyle>
		public GroupStyle GroupStyle { get; set; }

		#region Dependency Properties
		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register(
				"ItemsSource",
				typeof(object),
				typeof(ListViewBase),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((ListViewBase)s).OnItemsSourceChanged(e)
				)
			);

		public object ItemsSource
		{
			get { return (object)this.GetValue(ItemsSourceProperty); }
			set { this.SetValue(ItemsSourceProperty, value); }
		}

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(
				"SelectedItem",
				typeof(object),
				typeof(ListViewBase),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => (s as ListViewBase).OnSelectedItemChanged(e.OldValue, e.NewValue)
				)
			);

		public object SelectedItem
		{
			get { return (object)this.GetValue(SelectedItemProperty); }
			set { this.SetValue(SelectedItemProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
		public static DependencyProperty SelectedItemsProperty { get; } =
			DependencyProperty.Register(
				"SelectedItems",
				typeof(IList<object>),
				typeof(ListViewBase),
				new FrameworkPropertyMetadata(
					new List<object>(),
					(s, e) => ((ListViewBase)s)?.OnSelectedItemsChanged(e.OldValue as IList<object>, e.NewValue as IList<object>)
				)
			);

		public IList<object> SelectedItems
		{
			get { return (IList<object>)GetValue(SelectedItemsProperty); }
			set { SetValue(SelectedItemsProperty, value); }
		}

		public static DependencyProperty SelectionModeProperty { get; } =
			DependencyProperty.Register(
				"SelectionMode",
				typeof(ListViewSelectionMode),
				typeof(ListViewBase),
				new FrameworkPropertyMetadata(
					defaultValue: ListViewSelectionMode.Single,
					propertyChangedCallback: (s, e) => ((ListViewBase)s).OnSelectionModeChanged((ListViewSelectionMode)e.NewValue)
				)
			);

		public ListViewSelectionMode SelectionMode
		{
			get { return (ListViewSelectionMode)this.GetValue(SelectionModeProperty); }
			set { this.SetValue(SelectionModeProperty, value); }
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				"Header",
				typeof(object),
				typeof(ListViewBase),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((ListViewBase)s).OnHeaderChanged(e)
				)
			);

		public object Header
		{
			get { return (object)this.GetValue(HeaderProperty); }
			set { this.SetValue(HeaderProperty, value); }
		}

		public static DependencyProperty FooterProperty { get; } =
			DependencyProperty.Register(
				"Footer",
				typeof(object),
				typeof(ListViewBase),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((ListViewBase)s).OnFooterChanged(e)
				)
			);

		public object Footer
		{
			get { return (object)this.GetValue(FooterProperty); }
			set { this.SetValue(FooterProperty, value); }
		}

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
			DependencyProperty.Register("UnselectOnClick", typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(default(bool)));
		private ListViewBaseLayoutTemplate _layoutTemplate;
		#endregion
		#endregion

		protected ListViewBase(RectangleF frame, UICollectionViewLayout layout)
			: base(frame, layout)
		{
			Initialize();
		}

		private void Initialize()
		{
			BackgroundColor = UIColor.Clear;
			IFrameworkElementHelper.Initialize(this);
		}

		#region Overrides
		public override CGSize SizeThatFits(CGSize size)
		{
			return IFrameworkElementHelper.SizeThatFits(this, base.SizeThatFits(size));
		}
		#endregion

		#region Properties Changed
		protected virtual void OnSourceChanged(ListViewBaseSource oldSource, ListViewBaseSource newSource)
		{
			if (oldSource != null)
			{
				oldSource.SelectionChanged -= OnRouteSelectionChanged;
			}

			if (newSource != null)
			{
				newSource.Items = ItemsSource as IEnumerable;
				newSource.ItemClickCommand = Command;
				newSource.SelectionMode = SelectionMode;
				newSource.SelectionChanged += OnRouteSelectionChanged;
				newSource.ItemClick += (s2, e) => ItemClick?.Invoke(s2, e);
			}
		}

		private void OnRouteSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			e.OriginalSource = sender;

			if (e.AddedItems?.Any() ?? false)
			{
				ItemClick?.Invoke(this, new ItemClickEventArgs() { ClickedItem = e.AddedItems.FirstOrDefault() });
			}

			OnSelectionChanged(e);
		}

		partial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue)
		{
			if (newValue == Visibility.Visible)
			{
				SetItemSourceAndReload();
			}
		}

		private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
		{
			Source.Items = args.NewValue as IEnumerable;

			//We stop the execution here if the list is collapsed since we do not want to call a NeedsLayout for no reason.
			if (Visibility == Visibility.Collapsed)
			{
				return;
			}

			SetItemSourceAndReload();

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetSuperviewNeedsLayout());
		}

		private void SetItemSourceAndReload()
		{
			SetNeedsReloadData();

			if (SelectedItem != null && Source.Items.OfType<object>().None(item => SelectedItem.Equals(item)))
			{
				ResetSelection();
			}

			this.SetContentOffset(new PointF(0, 0), false);

			// This is required to ensure that the list will notify its
			// parents that its size may change because of some of its items.
			SetSuperviewNeedsLayout();

			ObserveCollectionChanged();
		}

		private void ObserveCollectionChanged()
		{
			var existingObservable = ItemsSource as INotifyCollectionChanged;

			//Subscribe to changes on ungrouped source
			if (existingObservable != null)
			{
				existingObservable.CollectionChanged += OnItemsSourceCollectionChanged;
				_notifyCollectionChanged.Disposable = Disposable.Create(() => existingObservable.CollectionChanged -= OnItemsSourceCollectionChanged);
			}
			//
			else if (Source.UnfilteredGroupedSource is INotifyCollectionChanged observableGroupedSource)
			{
				observableGroupedSource.CollectionChanged += OnItemsSourceGroupsChanged;
				_notifyCollectionChanged.Disposable = Disposable.Create(() => observableGroupedSource.CollectionChanged -= OnItemsSourceGroupsChanged);

			}
			else
			{
				_notifyCollectionChanged.Disposable = null;
			}

			//Subscribe to group changes if they are observable collections
			if (Source.UnfilteredGroupedSource is IEnumerable<INotifyCollectionChanged> observableGroups)
			{
				var disposables = new CompositeDisposable();
				int i = -1;
				foreach (var group in observableGroups)
				{
					//Hidden empty groups shouldn't be counted because they won't appear to UICollectionView
					if (!Source.AreEmptyGroupsHidden || (group as IEnumerable).Any())
					{
						i++;
					}
					var insideLoop = i;
					NotifyCollectionChangedEventHandler onCollectionChanged = (o, e) => OnItemsSourceSingleCollectionChanged(o, e, insideLoop);
					group.CollectionChanged += onCollectionChanged;
					Disposable.Create(() => group.CollectionChanged -= onCollectionChanged)
						.DisposeWith(disposables);
				}
				_notifyCollectionGroupsChanged.Disposable = disposables;
			}
			else
			{
				_notifyCollectionGroupsChanged.Disposable = null;
			}
		}

		private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			OnItemsSourceSingleCollectionChanged(sender, args, section: 0);
		}

		private void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args, int section)
		{
			if (_lastCollectionChangedActionWasReset || _needsLayoutAfterReloadData)
			{
				// If the collection was reset, or ReloadData() has been called for any other reason, we don't want to add/remove/move items, we want to reload the list.
				// This checks avoids NSInternalInconsistencyExceptions
				SetNeedsReloadData();
				_lastCollectionChangedActionWasReset = (args.Action == NotifyCollectionChangedAction.Reset);
				if (Source.AreEmptyGroupsHidden)
				{
					//Refresh subscriptions because the group occupancies may have changed.
					ObserveCollectionChanged();
				}
				return;
			}

			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (Source.AreEmptyGroupsHidden && (sender as IEnumerable).Count() == args.NewItems.Count)
					{
						//If HidesIfEmpty is true and a group becomes non-empty it is 'new' from the view of UICollectionView and we need to reset state
						SetNeedsReloadData();
						ObserveCollectionChanged();
						return;
					}
					InsertItems(GetIndexPathsFromStartAndCount(args.NewStartingIndex, args.NewItems.Count, section));
					break;
				case NotifyCollectionChangedAction.Remove:
					if (Source.AreEmptyGroupsHidden && (sender as IEnumerable).Count() == 0)
					{
						//If HidesIfEmpty is true and a group becomes empty it is 'vanished' from the view of UICollectionView and we need to reset state
						SetNeedsReloadData();
						ObserveCollectionChanged();
						return;
					}
					DeleteItems(GetIndexPathsFromStartAndCount(args.OldStartingIndex, args.OldItems.Count, section));
					break;
				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Replace:
					// TODO PBI #19974: Fully implement NotifyCollectionChangedActions and map them to the appropriate calls
					// on UICollectionView, MoveItems
					SetNeedsReloadData();
					break;
				case NotifyCollectionChangedAction.Reset:
					_lastCollectionChangedActionWasReset = true;
					SetNeedsReloadData();
					break;
			}

		}

		private void OnItemsSourceGroupsChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			//TODO: support adding/removing groups in-place (for now we just request a total refresh)
			SetNeedsReloadData();
			ObserveCollectionChanged();
		}

		private void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			SelectionChanged?.Invoke(this, e);
		}

		private void OnSelectedItemsChanged(IList<object> oldValue, IList<object> newValue)
		{
			OnSelectionChanged(new SelectionChangedEventArgs(this, oldValue, newValue));
			var newSafe = newValue.Safe();
			var oldSafe = oldValue.Safe();
			var newAndChanged = newSafe.Except(oldSafe);
			var removedAndChanged = oldSafe.Except(newSafe);

			foreach (var item in removedAndChanged)
			{
				UpdateItemSelectedState(item, false);
			}
			foreach (var item in newAndChanged)
			{
				UpdateItemSelectedState(item, true);
			}
		}

		private void UpdateItemSelectedState(object item, bool updateTo)
		{
			if (item == null)
			{
				// if item is null we can't update anything.
				return;
			}

			// Remark: CellForItem will return null if the cell is not visible.
			var cellToUpdate = this.IndexPathsForVisibleItems
				.Select(indexPath => (CellForItem(indexPath) as ListViewBaseSource.InternalContainer)?.Content)
				.FirstOrDefault(cellSelector => item.Equals(cellSelector?.DataContext));

			var selector = cellToUpdate as SelectorItem;

			if (selector != null)
			{
				selector.IsSelected = updateTo;
			}
		}

		private void OnSelectedItemChanged(object oldSelectedItem, object selectedItem)
		{
			SelectedItems = new[] { selectedItem }
				.ToArray();
		}

		private void OnSelectionModeChanged(ListViewSelectionMode newMode)
		{
			if (Source != null)
			{
				Source.SelectionMode = newMode;
			}

			switch (newMode)
			{
				case ListViewSelectionMode.Single:
					SelectedItems = new object[] { SelectedItem };
					break;

				case ListViewSelectionMode.None:
					ResetSelection();
					break;
			}
		}

		private void ResetSelection()
		{
			SelectedItem = null;
		}

		private void OnHeaderChanged(DependencyPropertyChangedEventArgs args)
		{
			SetSuperviewNeedsLayout();
		}

		private void OnFooterChanged(DependencyPropertyChangedEventArgs args)
		{
			SetSuperviewNeedsLayout();
		}
		#endregion

		/// <summary>
		/// Defines the layout to be used to display the list items.
		/// </summary>
		public virtual ListViewBaseLayout Layout
		{
			get
			{
				return (ListViewBaseLayout)CollectionViewLayout;
			}
			set
			{
				if (value != null)
				{
					value.Source = new WeakReference<ListViewBaseSource>(Source);
					value.Margin = Padding;
				}

				CollectionViewLayout = value;
			}
		}

		/// <summary>
		/// Defines the layout template to be used to display the items.
		/// </summary>
		public virtual ListViewBaseLayoutTemplate LayoutTemplate
		{
			get
			{
				return _layoutTemplate;
			}
			set
			{
				_layoutTemplate = value;

				if (value != null)
				{
					Layout = _layoutTemplate.LoadLayout();
				}
			}
		}

		public override void SetNeedsLayout()
		{
			base.SetNeedsLayout();

			// This method is present to ensure that it is not overridden by the mixins.
			// SetNeedsLayout is called very often during scrolling, and it must not
			// call SetParentNeeds layout, otherwise the whole visual tree above the listviewbase
			// will be refreshed.
		}

		public void ScrollIntoView(object item)
		{
			ScrollIntoView(item, ScrollIntoViewAlignment.Default);
		}

		public void ScrollIntoView(object item, ScrollIntoViewAlignment alignment)
		{
			//Check if item is a group
			if (Source.GroupedSource != null)
			{
				var groups = Source.GroupedSource.ToList();
				for (int i = 0; i < groups.Count; i++)
				{
					if (groups[i]?.Equals(item) ?? false)
					{
						//If item is a group, scroll to first element of that group
						ScrollToItem(NSIndexPath.FromItemSection(0, i), ConvertScrollAlignment(alignment), AnimateScrollIntoView);
						return;
					}
				}
			}

			var index = IndexPathForItem(item);
			if (index != null)
			{
				//Scroll to individual item, We set the UICollectionViewScrollPosition to None to have the same behavior as windows.
				//We can potentially customize this By using ConvertScrollAlignment and use different alignments.
				ScrollToItem(index, UICollectionViewScrollPosition.None, AnimateScrollIntoView);
			}
		}

		private UICollectionViewScrollPosition ConvertScrollAlignment(ScrollIntoViewAlignment alignment)
		{
			if (alignment == ScrollIntoViewAlignment.Default && this.Log().IsEnabled(LogLevel.Warning))
			{
				//TODO: UICollectionViewScrollPosition doesn't contain an option corresponding to 'nearest edge.' This might need to be implemented manually using ScrollRectToVisible
				this.Log().Warn("ScrollIntoViewAlignment.Default is not implemented");
			}

			var scrollDirection = (CollectionViewLayout as ListViewBaseLayout)?.ScrollDirection ?? ListViewBaseScrollDirection.Vertical;
			switch (scrollDirection)
			{
				case ListViewBaseScrollDirection.Horizontal:
					return UICollectionViewScrollPosition.Left;
				case ListViewBaseScrollDirection.Vertical:
					return UICollectionViewScrollPosition.Top;
			}

			throw new InvalidOperationException($"Invalid scroll direction: {scrollDirection}");
		}

		private NSIndexPath[] GetIndexPathsFromStartAndCount(int startIndex, int count, int section)
		{
			return Enumerable.Range(startIndex, count)
				.Select(index => NSIndexPath.FromItemSection(index, section))
				.ToArray();
		}

		/// <summary>
		/// Gets the NSIndexPath for the specified item
		/// </summary>
		/// <remark>Do not use when source is paginated as this iterates the source</remark>
		public NSIndexPath IndexPathForItem(object item)
		{
			if (Source.GroupedSource != null)
			{
				return IndexPathForItemInGroupedSource(item);
			}

			var index = Source.Items.IndexOf(item);

			// Item is not found
			if (index < 0)
			{
				return null;
			}

			return NSIndexPath.FromItemSection(index, section: 0);
		}

		private NSIndexPath IndexPathForItemInGroupedSource(object item)
		{
			var groups = Source.GroupedSource.ToList();
			for (int i = 0; i < groups.Count; i++)
			{
				var index = groups[i].IndexOf(item);
				if (index != -1)
				{
					return NSIndexPath.FromItemSection(index, i);
				}
			}

			//Item not found
			return null;
		}


		public override nint NumberOfItemsInSection(nint section)
		{
			return Source.GetItemsCount(this, section);
		}

		protected void SetNeedsReloadData()
		{
			//We do not want to reload if list is collapsed
			if (_needsReloadData || Visibility == Visibility.Collapsed)
			{
				return;
			}

			_needsReloadData = true;

			ReloadDataIfNeeded();
		}

		/// <summary>
		/// Reloads full list if need be
		/// </summary>
		internal void ReloadDataIfNeeded()
		{
			if (_needsReloadData)
			{
				_needsReloadData = false;
				_needsLayoutAfterReloadData = true;
				ReloadData();

				Layout?.ReloadData();
			}
		}

		internal void SetLayoutCreated()
		{
			_needsLayoutAfterReloadData = false;
		}

		public Thickness Padding
		{
			get { return Layout.Margin; }
			set { Layout.Margin = value; }
		}
	}
}
