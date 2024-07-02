using Uno.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using System.Collections.Specialized;
using Uno.Extensions.Specialized;
using System.Diagnostics;
using Uno.UI;
using Uno.Disposables;
using Windows.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;
using Windows.System;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Media;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class Selector : ItemsControl
	{
		private protected ScrollViewer m_tpScrollViewer;
		private protected bool _changingSelectedIndex;
		private protected bool _isUpdatingSelection;

		// The order at which the XAML properties are set on Selector should not matter.
		// SelectedItem might be set before ItemsSource is set, in which case the SelectedItem
		// value in m_itemPendingSelection until ItemsSource is set.
		private protected object m_itemPendingSelection;

		private protected IVirtualizingPanel VirtualizingPanel => ItemsPanelRoot as IVirtualizingPanel;

		private protected int m_iFocusedIndex;

		private protected bool m_skipScrollIntoView;

		public event SelectionChangedEventHandler SelectionChanged;

		private readonly SerialDisposable _collectionViewSubscription = new SerialDisposable();
		/// <summary>
		/// The path defined by <see cref="SelectedValuePath"/>, if it is set, otherwise null. This may be reused multiple times for
		/// checking <see cref="SelectedValue"/> candidates.
		/// </summary>
		private BindingPath _selectedValueBindingPath;
		private bool _disableRaiseSelectionChanged;
		//private int m_lastFocusedIndex;

		//private bool m_inCollectionChange;

		/// <summary>
		/// This is always true for <see cref="FlipView"/> and <see cref="ComboBox"/>, and depends on the value of <see cref="ListViewBase.SelectionMode"/> for <see cref="ListViewBase"/>.
		/// </summary>
		internal virtual bool IsSingleSelection => true;

		/// <summary>
		/// Templates for which it's known that the template root doesn't qualify as a container.
		/// </summary>
		private readonly HashSet<DataTemplate> _itemTemplatesThatArentContainers = new HashSet<DataTemplate>();

		public Selector()
		{

		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == SelectedItemProperty)
			{
				OnSelectedItemChanged(args.OldValue, args.NewValue, updateItemSelectedState: true);
			}
			else if (args.Property == SelectedIndexProperty)
			{
				OnSelectedIndexChanged((int)args.OldValue, (int)args.NewValue);
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			m_tpScrollViewer = GetTemplateChild<ScrollViewer>("ScrollViewer");
			if (m_tpScrollViewer is { })
			{
				m_tpScrollViewer.TemplatedParentHandlesScrolling = true;
			}
		}

		public static DependencyProperty SelectedItemProperty { get; } =
		DependencyProperty.Register(
			"SelectedItem",
			typeof(object),
			typeof(Selector),
			new FrameworkPropertyMetadata(defaultValue: null)
		);

		public object SelectedItem
		{
			get => this.GetValue(SelectedItemProperty);
			set => this.SetValue(SelectedItemProperty, value);
		}

		internal void OnSelectorItemIsSelectedChanged(SelectorItem container, bool oldIsSelected, bool newIsSelected)
		{
			var item = ItemFromContainer(container);

			ChangeSelectedItem(item, oldIsSelected, newIsSelected);
		}

		internal virtual void ChangeSelectedItem(object item, bool oldIsSelected, bool newIsSelected)
		{
			if (ReferenceEquals(SelectedItem, item) && !newIsSelected)
			{
				SelectedItem = null;
			}
			else if (!ReferenceEquals(SelectedItem, item) && newIsSelected)
			{
				SelectedItem = item;
			}
		}

		internal virtual void OnSelectedItemChanged(object oldSelectedItem, object selectedItem, bool updateItemSelectedState)
		{
			var wasSelectionUnset = oldSelectedItem == null && (!GetItems()?.Contains(null) ?? false);
			var isSelectionUnset = false;
			var items = GetItems();
			if (!items?.Contains(selectedItem) ?? false)
			{
				if (selectedItem == null)
				{
					isSelectionUnset = true;
				}
				else
				{
					if (ItemsSource is null)
					{
						m_itemPendingSelection = selectedItem;
					}

					var selectionToReset = items?.Contains(oldSelectedItem) ?? false ?
						oldSelectedItem
						// Note: in this scenario (previous SelectedItem no longer in collection either), Windows still leaves it at the
						// previous value. We don't try to reproduce this behaviour, amongst other reasons because under Uno's DP system
						// this would push an invalid value back through any 2-way binding.
						: null;
					try
					{
						_disableRaiseSelectionChanged = true;

						//Prevent SelectedItem being set to an invalid value
						SelectedItem = selectionToReset;
					}
					finally
					{
						_disableRaiseSelectionChanged = false;
					}

					return;
				}
			}

			var shouldRaiseSelectionChanged = !_isUpdatingSelection;
			_isUpdatingSelection = true;

			// If SelectedIndex is -1 and SelectedItem is being changed from non-null to null, this indicates that we're desetting
			// SelectedItem, not setting a null inside the collection as selected. Little edge case there. (Note that this relies
			// on user interactions setting SelectedIndex which then sets SelectedItem.)
			if (SelectedIndex == -1 && selectedItem == null)
			{
				isSelectionUnset = true;
			}

			var newIndex = -1;
			if (!_changingSelectedIndex)
			{
				newIndex = IndexFromItem(selectedItem);
				if (SelectedIndex != newIndex)
				{
					SelectedIndex = newIndex;
				}
			}

			OnSelectedItemChangedPartial(oldSelectedItem, selectedItem);

			UpdateSelectedValue();

			if (updateItemSelectedState && !_changingSelectedIndex)
			{
				TryUpdateSelectorItemIsSelected(oldSelectedItem, false);
				TryUpdateSelectorItemIsSelected(selectedItem, true);
			}

#if !IS_UNIT_TESTS
			if (newIndex != -1 && IsInLiveTree)
			{
				if (this is ListViewBase lvb)
				{
#if __IOS__ || __ANDROID__
					lvb.InstantScrollToIndex(newIndex);
#elif __MACOS__
					// not implemented
#else
					lvb.ScrollIntoView(selectedItem);
#endif
				}
			}
#endif

			_isUpdatingSelection = false;

			if (shouldRaiseSelectionChanged)
			{
				// Setting SelectedIndex above will have already invoked the SelectionChanged.
				InvokeSelectionChanged(
					wasSelectionUnset ? Array.Empty<object>() : new[] { oldSelectedItem },
					isSelectionUnset ? Array.Empty<object>() : new[] { selectedItem }
				);
			}
		}

		internal void TryUpdateSelectorItemIsSelected(object item, bool isSelected)
		{
			if (item is SelectorItem si)
			{
				si.IsSelected = isSelected;
			}
			else if (ContainerFromItem(item) is SelectorItem si2)
			{
				si2.IsSelected = isSelected;
			}
		}

		internal bool DisableRaiseSelectionChanged
		{
			get => _disableRaiseSelectionChanged;
			set => _disableRaiseSelectionChanged = value;
		}

		private void UpdateSelectedValue()
		{
			if (!SelectedValuePath.IsNullOrEmpty())
			{
				if (_selectedValueBindingPath?.Path != SelectedValuePath)
				{
					_selectedValueBindingPath = new Uno.UI.DataBinding.BindingPath(SelectedValuePath, null);
				}
			}
			else
			{
				_selectedValueBindingPath = null;
			}


			if (_selectedValueBindingPath != null)
			{
				_selectedValueBindingPath.DataContext = SelectedItem;
				SelectedValue = _selectedValueBindingPath.Value;
			}
			else
			{
				SelectedValue = SelectedItem;
			}
		}


		partial void OnSelectedItemChangedPartial(object oldSelectedItem, object selectedItem);

		internal void InvokeSelectionChanged(object[] removedItems, object[] addedItems)
		{
			if (!_disableRaiseSelectionChanged)
			{
				SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, removedItems, addedItems));
			}
		}

		public int SelectedIndex
		{
			get => (int)this.GetValue(SelectedIndexProperty);
			set => this.SetValue(SelectedIndexProperty, value);
		}

		public static DependencyProperty SelectedIndexProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedIndex),
				typeof(int),
				typeof(Selector),
				new FrameworkPropertyMetadata(-1, coerceValueCallback: CoerceSelectedIndex));

		private int _uncoercedSelectedIndex = -1;

		private static object CoerceSelectedIndex(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			if (baseValue is not int desiredIndex)
			{
				return -1;
			}

			var owner = (Selector)dependencyObject;
			if (desiredIndex == -1)
			{
				owner._uncoercedSelectedIndex = -1;
				return -1;
			}

			var itemCount = owner.NumberOfItems;
			if (itemCount > 0)
			{
				// Some items already exist.
				// We validate bounds and throw if index differs from uncoerced.
				if (desiredIndex < -1 || desiredIndex >= itemCount)
				{
					if (desiredIndex != owner._uncoercedSelectedIndex)
					{
						throw new ArgumentException(
							nameof(baseValue),
							"SelectedIndex cannot be set to invalid value when items are present");
					}
					else
					{
						// Ignore change.
						return -1;
					}
				}
				owner._uncoercedSelectedIndex = -1;
				return desiredIndex;
			}
			else
			{
				// No items exist, store uncoerced and set to -1;
				owner._uncoercedSelectedIndex = desiredIndex;
				return -1;
			}
		}

		internal virtual void OnSelectedIndexChanged(int oldSelectedIndex, int newSelectedIndex)
		{
			try
			{
				_changingSelectedIndex = true;
				var shouldRaiseSelectionChanged = !_isUpdatingSelection;
				_isUpdatingSelection = true;
				var oldSelectedItem = SelectedItem;
				var newSelectedItem = ItemFromIndex(newSelectedIndex);

				if (ItemsSource is ICollectionView collectionView)
				{
					collectionView.MoveCurrentToPosition(newSelectedIndex);
					//TODO: we should check if CurrentPosition actually changes, and set SelectedIndex back if not.
				}
				if (!object.ReferenceEquals(oldSelectedItem, newSelectedItem))
				{
					SelectedItem = newSelectedItem;
				}

				SelectedIndexPath = GetIndexPathFromIndex(SelectedIndex);
				_isUpdatingSelection = false;
				if (shouldRaiseSelectionChanged)
				{
					InvokeSelectionChanged(
						oldSelectedIndex == -1 ? Array.Empty<object>() : new[] { oldSelectedItem },
						newSelectedIndex == -1 ? Array.Empty<object>() : new[] { newSelectedItem });
				}
			}
			finally
			{
				_changingSelectedIndex = false;
			}
		}

		public string SelectedValuePath
		{
			get => (string)this.GetValue(SelectedValuePathProperty);
			set => this.SetValue(SelectedValuePathProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty SelectedValuePathProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(SelectedValuePath),
			propertyType: typeof(string),
			ownerType: typeof(Selector),
			typeMetadata: new FrameworkPropertyMetadata("", propertyChangedCallback: (s, e) => (s as Selector)?.UpdateSelectedValue())
		);

		public object SelectedValue
		{
			get => this.GetValue(SelectedValueProperty);
			set => this.SetValue(SelectedValueProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty SelectedValueProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(SelectedValue),
			propertyType: typeof(object),
			ownerType: typeof(Selector),
			typeMetadata: new FrameworkPropertyMetadata(null, (s, e) => (s as Selector).OnSelectedValueChanged(e.OldValue, e.NewValue), SelectedValueCoerce)
		);

		private void OnSelectedValueChanged(object oldValue, object newValue)
		{
			if (_changingSelectedIndex)
			{
				return;
			}

			var (indexOfItemWithValue, itemWithValue) = FindIndexOfItemWithValue(newValue);
			SelectedIndex = indexOfItemWithValue;
		}

		private static object SelectedValueCoerce(DependencyObject snd, object baseValue, DependencyPropertyValuePrecedences _)
		{
			var selector = (Selector)snd;
			if (selector?._selectedValueBindingPath != null)
			{
				return baseValue; // Setting the SelectedValue won't update the index when a _path is used.
			}
			return selector.GetItems()?.Contains(baseValue) ?? false ? baseValue : null;
		}

		public bool? IsSynchronizedWithCurrentItem
		{
			get => (bool?)GetValue(IsSynchronizedWithCurrentItemProperty);
			set => SetValue(IsSynchronizedWithCurrentItemProperty, value);
		}

		public static DependencyProperty IsSynchronizedWithCurrentItemProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				nameof(IsSynchronizedWithCurrentItem),
				typeof(bool?),
				typeof(Selector),
				new FrameworkPropertyMetadata(
					default(bool?),
					propertyChangedCallback: (s, e) => (s as Selector)?.IsSynchronizedWithCurrentItemChanged((bool?)e.OldValue, (bool?)e.NewValue)));

		private void IsSynchronizedWithCurrentItemChanged(bool? oldValue, bool? newValue)
		{
			if (newValue == true)
			{
				throw new ArgumentOutOfRangeException("True is not a supported value for this property");
			}

			TrySubscribeToCurrentChanged();
		}

		/// <summary>
		/// The selected index as an <see cref="Uno.UI.IndexPath"/> of (group, group position), where group=0 if the source is ungrouped.
		/// </summary>
		private Uno.UI.IndexPath? SelectedIndexPath { get; set; }

		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);
			TrySubscribeToCurrentChanged();
			Refresh();

			if (e.NewValue is { } && m_itemPendingSelection is { })
			{
				bool itemFound;

				var items = GetItems();

				// Check if we're trying to restore a value that's not in the collection.
				itemFound = items.IndexOf(m_itemPendingSelection) != -1;
				if (itemFound)
				{
					SelectedItem = m_itemPendingSelection;
				}

				m_itemPendingSelection = null;
			}
		}

		private void TrySubscribeToCurrentChanged()
		{
			var trackCurrentItem = IsSynchronizedWithCurrentItem ?? true;

			if (ItemsSource is ICollectionView collectionView && trackCurrentItem)
			{
				// This is a workaround to support the use of EventRegistrationTokenTable in consumer code. EventRegistrationTokenTable
				// currently has a bug on Xamarin that prevents instance methods subscribed directly from ever being unsubscribed.
				EventHandler<object> currentChangedHandler = OnCollectionViewCurrentChanged;
				_collectionViewSubscription.Disposable = Disposable.Create(() => collectionView.CurrentChanged -= currentChangedHandler);
				collectionView.CurrentChanged += currentChangedHandler;
				SelectedIndex = collectionView.CurrentPosition;
			}
			else
			{
				_collectionViewSubscription.Disposable = null;

				if (IsSynchronizedWithCurrentItem is { } value && !value)
				{
					ResetIndexIfNeeded();
				}
			}
		}

		private void OnCollectionViewCurrentChanged(object sender, object e)
		{
			if (IsSingleSelection)
			{
				if (ItemsSource is ICollectionView collectionView)
				{
					SelectedIndex = collectionView.CurrentPosition;
				}
			}
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			if (element is SelectorItem selectorItem)
			{
				selectorItem.IsSelected = IsSelected(IndexFromContainer(element));
			}
		}

		internal virtual bool IsSelected(int index)
		{
			return SelectedIndex == index;
		}

		internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs c, int section)
		{
			// Get SelectedIndex before calling base, as a quick workaround for side-effect in FlipView.Android that changes the SelectedIndex, to avoid incrementing it twice
			var selectedIndexToSet = SelectedIndex;
			base.OnItemsSourceSingleCollectionChanged(sender, c, section);
			switch (c.Action)
			{
				case NotifyCollectionChangedAction.Add:

					//Advance SelectedIndex if items are being inserted before it
					var newIndex = GetIndexFromIndexPath(Uno.UI.IndexPath.FromRowSection(c.NewStartingIndex, section));
					if (selectedIndexToSet >= newIndex)
					{
						selectedIndexToSet += c.NewItems.Count;
						SelectedIndex = Math.Min(NumberOfItems - 1, selectedIndexToSet);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					{
						var oldIndex = GetIndexFromIndexPath(Uno.UI.IndexPath.FromRowSection(c.OldStartingIndex, section));
						if (selectedIndexToSet >= oldIndex && selectedIndexToSet < oldIndex + c.OldItems.Count)
						{
							//Deset if selected item is being removed
							ResetIndexIfNeeded();
						}
						else if (selectedIndexToSet >= oldIndex + c.OldItems.Count)
						{
							//Decrement SelectedIndex if items are removed before it
							selectedIndexToSet -= c.OldItems.Count;
							SelectedIndex = Math.Max(-1, selectedIndexToSet);
						}
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					{
						var oldIndex = GetIndexFromIndexPath(Uno.UI.IndexPath.FromRowSection(c.OldStartingIndex, section));
						if (selectedIndexToSet >= oldIndex && selectedIndexToSet < oldIndex + c.OldItems.Count)
						{
							//Deset if selected item is being replaced
							ResetIndexIfNeeded();
						}
					}
					break;
				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Reset:
					if (SelectedIndexPath?.Section == section)
					{
						var item = ItemFromIndex(SelectedIndex);
						if (item == null || !item.Equals(SelectedItem))
						{
							// If selected item and index no longer coincide, unselect the selection
							ResetIndexIfNeeded();
						}
					}
					break;
			}

			OnItemsChanged(c);
		}

		internal override void OnItemsSourceGroupsChanged(object sender, NotifyCollectionChangedEventArgs c)
		{
			base.OnItemsSourceGroupsChanged(sender, c);

			if (SelectedIndexPath == null)
			{
				return;
			}
			var selectedIndexPath = SelectedIndexPath.Value;

			switch (c.Action)
			{
				case NotifyCollectionChangedAction.Add:
					var newIndexPath = Uno.UI.IndexPath.FromRowSection(row: 0, section: c.NewStartingIndex);
					if (selectedIndexPath >= newIndexPath)
					{
						//Advance SelectedIndex if groups are being inserted before it
						var newSelectedIndex = SelectedIndex;
						for (int i = c.NewStartingIndex; i < c.NewStartingIndex + c.NewItems.Count; i++)
						{
							newSelectedIndex += GetGroupCount(i);
						}
						SelectedIndex = newSelectedIndex;
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					{
						var oldIndexPath = Uno.UI.IndexPath.FromRowSection(row: 0, section: c.OldStartingIndex);
						var oldIndexPathLast = Uno.UI.IndexPath.FromRowSection(row: int.MaxValue, section: c.OldStartingIndex + c.OldItems.Count);
						if (selectedIndexPath >= oldIndexPath && selectedIndexPath < oldIndexPathLast)
						{
							//Deset if selected item is in group being removed
							ResetIndexIfNeeded();
						}
						else if (selectedIndexPath >= oldIndexPathLast)
						{
							//Decrement SelectedIndex if groups are removed before it
							var newSelectedIndex = SelectedIndex;
							for (int i = c.OldStartingIndex; i < c.OldStartingIndex + c.OldItems.Count; i++)
							{
								newSelectedIndex -= GetGroupCount(i);
							}
							SelectedIndex = newSelectedIndex;
						}
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					{
						var oldIndexPath = Uno.UI.IndexPath.FromRowSection(row: 0, section: c.OldStartingIndex);
						var oldIndexPathLast = Uno.UI.IndexPath.FromRowSection(row: int.MaxValue, section: c.OldStartingIndex + c.OldItems.Count);
						if (selectedIndexPath >= oldIndexPath && selectedIndexPath < oldIndexPathLast)
						{
							//Deset if selected item is in group being replaced
							ResetIndexIfNeeded();
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					ResetIndexIfNeeded();
					break;
			}
		}

		internal void OnItemClicked(SelectorItem selectorItem, VirtualKeyModifiers modifiers) => OnItemClicked(IndexFromContainer(selectorItem), modifiers);

		internal virtual void OnItemClicked(int clickedIndex, VirtualKeyModifiers modifiers)
		{
			if (ItemsSource is ICollectionView collectionView)
			{
				//NOTE: Windows seems to call MoveCurrentTo(item); we set position instead to have expected behavior when you have duplicate items in the list.
				collectionView.MoveCurrentToPosition(clickedIndex);

				// The CollectionView may have intercepted the change
				clickedIndex = collectionView.CurrentPosition;
			}

			SelectedIndex = clickedIndex;
		}

		private void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			if (
				// When ItemsSource is set, we get collection changes from it directly (and it's not possible to directly modify Items)
				ItemsSource == null
			)
			{
				var iVCE = e.ToVectorChangedEventArgs();

				if (iVCE.CollectionChange == CollectionChange.ItemChanged
					|| (iVCE.CollectionChange == CollectionChange.ItemInserted && iVCE.Index < Items.Count))
				{
					var item = Items[(int)iVCE.Index];

					if (item is SelectorItem selectorItem && selectorItem.IsSelected)
					{
						ChangeSelectedItem(selectorItem, false, true);
					}
				}
				//Prevent SelectedIndex been >= Items.Count
				var sItem = ItemFromIndex(SelectedIndex);

				if (sItem == null || !sItem.Equals(SelectedItem))
				{
					ResetIndexIfNeeded();
				}
			}

			if (_uncoercedSelectedIndex > -1 && _uncoercedSelectedIndex < NumberOfItems)
			{
				SelectedIndex = _uncoercedSelectedIndex;
			}
		}

		/// <summary>
		/// Sets SelectedIndex to -1 if not already set to this value.
		/// This is needed to ensure Selector code does not clear the uncoerced value.
		/// </summary>
		private void ResetIndexIfNeeded()
		{
			if (SelectedIndex != -1)
			{
				SelectedIndex = -1;
			}
		}

		// Check if the root of the resolved item template qualifies as a container, and if so return it as the container.
		private protected override DependencyObject GetRootOfItemTemplateAsContainer(DataTemplate template)
		{
			if (_itemTemplatesThatArentContainers.Contains(template))
			{
				// We have seen this template before and it didn't qualify as a container, no need to materialize it
				return null;
			}

			var templateRoot = template?.LoadContentCached();

			if (IsItemItsOwnContainerOverride(templateRoot))
			{
				if (templateRoot is ContentControl contentControl)
				{
					// The container has been created from a template and can be recycled, so we mark it as generated
					contentControl.IsGeneratedContainer = true;

					contentControl.IsContainerFromTemplateRoot = true;
				}

				return templateRoot as DependencyObject;
			}

			if (templateRoot != null)
			{
				_itemTemplatesThatArentContainers.Add(template);
				template.ReleaseTemplateRoot(templateRoot);
			}

			return null;
		}

		/// Finds the index of the first item with a given property path value.
		(int Index, object ItemWithValue) FindIndexOfItemWithValue(object value)
		{
			var nCount = NumberOfItems;

			for (int cnt = 0; cnt < nCount; cnt++)
			{
				var item = GetItemFromIndex(cnt);
				var itemValue = GetSelectedValue(item);
				if (Equals(value, itemValue))
				{
					return (cnt, itemValue);
				}
			}

			return (-1, null);
		}

		/// Returns the selected value of an item using a path.
		object GetSelectedValue(object pItem)
		{
			if (_selectedValueBindingPath == null || pItem == null)
			{
				return pItem;
			}

			_selectedValueBindingPath.DataContext = pItem;
			return _selectedValueBindingPath.Value;
		}

		private protected override void Refresh()
		{
			base.Refresh();
			_itemTemplatesThatArentContainers.Clear();
			RefreshPartial();
		}

		public bool IsSelectionActive { get; set; }

		protected virtual (Orientation PhysicalOrientation, Orientation LogicalOrientation) GetItemsHostOrientations()
		{
			return (Orientation.Horizontal, Orientation.Horizontal);
		}

		protected void SetFocusedItem(int index,
									  bool shouldScrollIntoView,
									  bool forceFocus,
									  FocusState focusState,
									  bool animateIfBringIntoView)
		{

		}

		protected void SetFocusedItem(int index,
									  bool shouldScrollIntoView,
									  bool forceFocus,
									  FocusState focusState,
									  bool animateIfBringIntoView,
									  FocusNavigationDirection focusNavigationDirection)
		{

			//bool bFocused = false;
			//bool shouldFocus = false;

			//var spItems = Items;
			//var nCount = spItems?.Size;

			//if (index < 0 || nCount <= index)
			//{
			//	index = -1;
			//}

			//if (index >= 0)
			//{
			//	m_lastFocusedIndex = index;
			//}

			//if (!forceFocus)
			//{
			//	//shouldFocus = HasFocus();
			//}
			//else
			//{
			//	shouldFocus = true;
			//}

			//if (shouldFocus)
			//{
			//	m_iFocusedIndex = index;
			//}

			//if (m_iFocusedIndex == -1)
			//{
			//	if (shouldFocus)
			//	{
			//		// Since none of our child items have the focus, put the focus back on the main list box.
			//		//
			//		// This will happen e.g. when the focused item is being removed but is still in the visual tree at the time of this call.
			//		// Note that this call may fail e.g. if IsTabStop is false, which is OK; it will just set focus
			//		// to the next focusable element (or clear focus if none is found).
			//		bFocused = Focus(focusState);
			//	}

			//	return;
			//}

			//if (shouldScrollIntoView)
			//{
			//	shouldScrollIntoView = CanScrollIntoView();
			//}

			//if (shouldScrollIntoView)
			//{
			//	ScrollIntoView(
			//		index,
			//		isGroupItemIndex: false,
			//		isHeader: false,
			//		isFooter: false,
			//		isFromPublicAPI: false,
			//		ensureContainerRealized: true,
			//		animateIfBringIntoView,
			//		ScrollIntoViewAlignment.Default);
			//}

			//if (shouldFocus)
			//{
			//	var spContainer = ContainerFromIndex(index);

			//	if (spContainer is SelectorItem spSelectorItem)
			//	{
			//		//spSelectorItem.FocusSelfOrChild(focusState, animateIfBringIntoView, &bFocused, focusNavigationDirection);
			//	}
			//}
		}

#if false
		private void ScrollIntoView(int index, bool isGroupItemIndex, bool isHeader, bool isFooter, bool isFromPublicAPI, bool ensureContainerRealized, bool animateIfBringIntoView, ScrollIntoViewAlignment @default)
		{

		}
#endif

		protected void SetFocusedItem(int index,
									  bool shouldScrollIntoView,
									  bool animateIfBringIntoView,
									  FocusNavigationDirection focusNavigationDirection)
		{

			//bool hasFocus = false;
			FocusState focusState = FocusState.Programmatic;

			//hasFocus = HasFocus();

			//if (hasFocus)
			//{
			//	DependencyObject spFocused;

			//	//spFocused = GetFocusedElement();

			//	if (spFocused is UIElement spFocusedAsElement)
			//	{
			//		focusState = spFocusedAsElement.FocusState;
			//	}
			//}

			SetFocusedItem(index,
							shouldScrollIntoView,
							forceFocus: false,
							focusState,
							animateIfBringIntoView,
							focusNavigationDirection);
		}

		protected void SetFocusedItem(int index,
									  bool shouldScrollIntoView)
		{

		}

#if false
		bool CanScrollIntoView()
		{
			Panel spPanel;
			bool isItemsHostInvalid = false;
			bool isInLiveTree = false;

			spPanel = ItemsPanelRoot;

			if (spPanel != null)
			{
				//isItemsHostInvalid = IsItemsHostInvalid;

				if (!isItemsHostInvalid)
				{
					//isInLiveTree = IsInLiveTree();
					isInLiveTree = true;
				}
			}

			return !isItemsHostInvalid && isInLiveTree && !m_skipScrollIntoView && !m_inCollectionChange;
		}
#endif

		partial void RefreshPartial();


		/// <summary>
		/// Is the managed implementation for virtualized lists (ItemsStackPanel, ItemsWrapGrid, CarouselPanel etc) used on this platform?
		/// </summary>
		internal const bool UsesManagedLayouting =
#if __IOS__ || __ANDROID__
			false;
#else
			true;
#endif
	}
}
