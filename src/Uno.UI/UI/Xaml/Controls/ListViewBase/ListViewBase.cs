using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
#if __ANDROID__
using _View = Android.Views.View;
#elif __APPLE_UIKIT__
using _View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.FrameworkElement;
#endif
using Uno;
using Uno.Extensions;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Uno.Extensions.Specialized;
using System.Collections;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.Disposables;
using Uno.Client;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;
using Uno.UI;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ListViewBase : Selector
	{
		/// <summary>
		/// When this flag is set, the ListViewBase will process every notification from <see cref="INotifyCollectionChanged"/> as if it 
		/// were a 'Reset', triggering a complete refresh of the list. By default this is false.
		/// </summary>
		public bool RefreshOnCollectionChanged { get; set; }

		internal override bool IsSingleSelection => SelectionMode == ListViewSelectionMode.Single;
		internal bool IsSelectionMultiple => SelectionMode == ListViewSelectionMode.Multiple || SelectionMode == ListViewSelectionMode.Extended;
		private bool _modifyingSelectionInternally;
		private readonly List<object> _oldSelectedItems = new List<object>();

		/// <summary>
		/// Whether an incremental data loading request is currently under way.
		/// </summary>
		private bool _isIncrementalLoadingInFlight;

		private readonly Dictionary<DependencyObject, object> _containersForIndexRepair = new Dictionary<DependencyObject, object>();

		protected internal ListViewBase()
		{
			Initialize();

			var selectedItems = new ObservableCollection<object>();
			selectedItems.CollectionChanged += OnSelectedItemsCollectionChanged;
			SelectedItems = selectedItems;
		}

		public event TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> ContainerContentChanging;

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (!HasItems)
			{
				// If there are no items in the source, try to load at least one.
				TryLoadFirstItem();
			}
			return base.ArrangeOverride(finalSize);
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			args.Handled = TryHandleKeyDown(args);
		}

		internal bool TryHandleKeyDown(KeyRoutedEventArgs args)
		{
			if (!IsEnabled)
			{
				return false;
			}

			var focusedElement = XamlRoot is { } xamlRoot ?
				FocusManager.GetFocusedElement(XamlRoot) :
				null;
			var focusedContainer = focusedElement as SelectorItem;

			if (args.Key == VirtualKey.Enter ||
				args.Key == VirtualKey.Space)
			{
				// Invoke focused
				if (focusedContainer != null)
				{
					OnItemClicked(focusedContainer, args.KeyboardModifiers);
				}

#if __WASM__
				((IHtmlHandleableRoutedEventArgs)args).HandledResult &= ~HtmlEventDispatchResult.PreventDefault;
#endif
				return true;
			}
			else
			{
				var orientation = ItemsPanelRoot?.PhysicalOrientation ?? Orientation.Vertical;

				switch (args.Key)
				{
					case VirtualKey.Down when orientation == Orientation.Vertical:
						return TryMoveKeyboardFocusAndSelection(+1, args.KeyboardModifiers);
					case VirtualKey.Up when orientation == Orientation.Vertical:
						return TryMoveKeyboardFocusAndSelection(-1, args.KeyboardModifiers);
					case VirtualKey.Right when orientation == Orientation.Horizontal:
						return TryMoveKeyboardFocusAndSelection(+1, args.KeyboardModifiers);
					case VirtualKey.Left when orientation == Orientation.Horizontal:
						return TryMoveKeyboardFocusAndSelection(-1, args.KeyboardModifiers);
					default:
						return false;
				}
			}
		}

		private bool TryMoveKeyboardFocusAndSelection(int offset, VirtualKeyModifiers modifiers)
		{
			var (prevIndex, prevContainer, prevItem) = FocusedIndexContainerItem;

			var newIndex = prevIndex + offset;
			var newContainer = ContainerFromIndex(newIndex) as SelectorItem;
			var newItem = ItemFromIndex(newIndex);

			if (newIndex < 0 || newIndex >= Items.Count)
			{
				return false;
			}

			FocusedIndexContainerItem = (newIndex, newContainer, newItem);

			switch (SelectionMode)
			{
				case ListViewSelectionMode.None:
					break;
				case ListViewSelectionMode.Single:
					if (SingleSelectionFollowsFocus && !modifiers.HasFlag(VirtualKeyModifiers.Control))
					{
						SelectedIndex = newIndex;
					}
					break;
				case ListViewSelectionMode.Multiple:
					if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
					{
						// if shift is held, the newly-focused item matches the selection
						// of the previously-focused item
						if (IsSelected(prevContainer, prevItem))
						{
							SelectInMultipleSelection(newItem, newContainer);
						}
						else
						{
							UnselectInMultipleSelection(newItem, newContainer);
						}
					}

					// otherwise just focus, don't select
					break;
				case ListViewSelectionMode.Extended:
					ExtendedSelectionCase();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			// StartBringIntoView shouldn't be needed, since the internal ScrollViewer has BringIntoViewOnFocusChange
			// but since that property isn't currently supported, we have to manually BringIntoView.
			newContainer?.StartBringIntoView(new BringIntoViewOptions()
			{
				AnimationDesired = false
			});
			newContainer?.Focus(FocusState.Keyboard);
			return true;

			void ExtendedSelectionCase()
			{

				if (modifiers.HasFlag(VirtualKeyModifiers.Control))
				{
					// just focus, don't select
					return;
				}

				if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					// if shift is held, all items between the clicked item and the "selection
					// start" are selected. Everything else is unselected.
					var lowerBound = Math.Min(newIndex, ExtendedShiftSelectionStart);
					var upperBound = Math.Max(newIndex, ExtendedShiftSelectionStart);

					for (var currentIndex = 0; currentIndex < Items.Count; currentIndex++)
					{
						var currentItem = ItemFromIndex(currentIndex);
						var currentContainer = ContainerFromItem(currentItem) as SelectorItem;

						if (currentIndex > upperBound || currentIndex < lowerBound)
						{
							UnselectInMultipleSelection(currentItem, currentContainer);
						}
						else
						{
							SelectInMultipleSelection(currentItem, currentContainer);
						}
					}

					return;
				}

				// no modifiers

				// Note: In pointer moves, ctrl-moves also move the ExtendedShiftSelectionStart
				// but here, only non-modifier moves do
				ExtendedShiftSelectionStart = newIndex;

				// Act like single selection mode i.e. set one, unset everything else
				for (var currentIndex = 0; currentIndex < Items.Count; currentIndex++)
				{
					if (currentIndex != newIndex)
					{
						var currentItem = ItemFromIndex(currentIndex);
						var currentContainer = ContainerFromItem(currentItem) as SelectorItem;

						UnselectInMultipleSelection(currentItem, currentContainer);
					}
				}

				SelectInMultipleSelection(newItem, newContainer);
			}
		}

		private bool IsSelected(SelectorItem container, object item) => container?.IsSelected ?? SelectedItems.Contains(item);

		private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_modifyingSelectionInternally)
			{
				//Internal operations that modify the list are responsible for invoking SelectionChanged, etc if necessary
				_oldSelectedItems.Clear();
				_oldSelectedItems.AddRange(SelectedItems);
				return;
			}

			var items = GetItems();
			if (items == null && _oldSelectedItems.Any())
			{
				// The list is being reset therefore we just need to reset the selection
				ResetSelection();
				return;
			}

			object[] validAdditions;
			object[] validRemovals;
			if (e.Action != NotifyCollectionChangedAction.Reset)
			{
				validAdditions = (e.NewItems ?? Array.Empty<object>()).Where(item => items.Contains(item)).ToObjectArray();
				validRemovals = (e.OldItems ?? Array.Empty<object>()).Where(item => items.Contains(item)).ToObjectArray();
			}
			else
			{
				validAdditions = Array.Empty<object>();
				validRemovals = _oldSelectedItems.Where(item => items.Contains(item)).ToObjectArray();
			}
			try
			{
				_modifyingSelectionInternally = true;
				_isUpdatingSelection = true;
				var itemIndex = SelectedItems.Select(item => (int?)items.IndexOf(item)).FirstOrDefault(index => index > -1);
				if (itemIndex != null)
				{
					SelectedItem = items.ElementAt(itemIndex.Value);
					SelectedIndex = itemIndex.Value;
				}
				else
				{
					SelectedItem = null;
					SelectedIndex = -1;
				}

				TryUpdateSelectorItemIsSelected(validRemovals, false);
				TryUpdateSelectorItemIsSelected(validAdditions, true);
			}
			finally
			{
				_modifyingSelectionInternally = false;
				_isUpdatingSelection = false;
			}
			if (validAdditions.Any() || validRemovals.Any())
			{
				InvokeSelectionChanged(validRemovals, validAdditions);
			}
			_oldSelectedItems.Clear();
			_oldSelectedItems.AddRange(SelectedItems);
		}

		private void TryUpdateSelectorItemIsSelected(object[] items, bool isSelected)
		{
			foreach (var added in items)
			{
				TryUpdateSelectorItemIsSelected(added, isSelected);
			}
		}

		private void ResetSelection()
		{
			try
			{
				_modifyingSelectionInternally = true;

				_oldSelectedItems.Clear();
				_oldSelectedItems.AddRange(SelectedItems);
				SelectedItems.Clear();
			}
			finally
			{
				_modifyingSelectionInternally = false;
			}
		}

		internal override void ChangeSelectedItem(object item, bool oldIsSelected, bool newIsSelected)
		{
			if (!_modifyingSelectionInternally)
			{

				//Handle selection
				switch (SelectionMode)
				{
					case ListViewSelectionMode.None:
						break;
					case ListViewSelectionMode.Single:
						if (_changingSelectedIndex)
						{
							break;
						}

						var index = IndexFromItem(item);
						if (!newIsSelected)
						{
							if (SelectedIndex == index)
							{
								SelectedIndex = -1;
							}
						}
						else
						{
							SelectedIndex = index;
						}
						break;
					case ListViewSelectionMode.Multiple:
					case ListViewSelectionMode.Extended:
						if (!newIsSelected)
						{
							SelectedItems.Remove(item);
						}
						else
						{
							if (!SelectedItems.Contains(item))
							{
								SelectedItems.Add(item);
							}
						}
						break;
				}
			}
		}

		internal override void OnSelectedItemChanged(object oldSelectedItem, object selectedItem, bool updateItemSelectedState)
		{
			if (_modifyingSelectionInternally)
			{
				return;
			}
			if (IsSelectionMultiple)
			{
				var items = GetItems();
				if (selectedItem == null || items.Contains(selectedItem))
				{
					object[] removedItems = null;
					object[] addedItems = null;
					try
					{
						_modifyingSelectionInternally = true;
						removedItems = SelectedItems.Except(selectedItem).ToObjectArray();
						var isRealSelection = selectedItem != null || items.Contains(null);
						addedItems = SelectedItems.Contains(selectedItem) || !isRealSelection ? Array.Empty<object>() : new[] { selectedItem };
						SelectedItems.Clear();
						if (isRealSelection)
						{
							SelectedItems.Add(selectedItem);
						}
					}
					finally
					{
						_modifyingSelectionInternally = false;
					}
					//Invoke event after resetting flag, in case callbacks in user code modify the collection
					if (addedItems.Length > 0 || removedItems.Length > 0)
					{
						InvokeSelectionChanged(removedItems, addedItems);
					}
				}
				else
				{
					SelectedItem = oldSelectedItem;
				}
			}
			else
			{
				try
				{
					_modifyingSelectionInternally = true;

					if (selectedItem != null)
					{
						SelectedItems.Update(new[] { selectedItem });
					}
					else
					{
						SelectedItems.Clear();
					}
				}
				finally
				{
					_modifyingSelectionInternally = false;
				}

				base.OnSelectedItemChanged(
					oldSelectedItem: oldSelectedItem,
					selectedItem: selectedItem,
					updateItemSelectedState: true);
			}
		}

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// In Single mode, we respond to SelectedIndex changing, which is more reliable if there are duplicate items
			if (IsSelectionMultiple)
			{
				if (e.AddedItems is not null)
				{
					foreach (var item in e.AddedItems)
					{
						SetSelectedState(IndexFromItem(item), true);
					}
				}

				if (e.RemovedItems is not null)
				{
					foreach (var item in e.RemovedItems)
					{
						SetSelectedState(IndexFromItem(item), false);
					}
				}
			}
		}

		internal override void OnSelectedIndexChanged(int oldSelectedIndex, int newSelectedIndex)
		{
			base.OnSelectedIndexChanged(oldSelectedIndex, newSelectedIndex);
			if (ExtendedShiftSelectionStart == -1)
			{
				// only changed if it wasn't already set. This matches the behaviour on WinUI.
				ExtendedShiftSelectionStart = newSelectedIndex;
			}

			try
			{
				_changingSelectedIndex = true;
				SetSelectedState(oldSelectedIndex, false);
				SetSelectedState(newSelectedIndex, true);
			}
			finally
			{
				_changingSelectedIndex = false;
			}
		}

		public event ItemClickEventHandler ItemClick;

		private void Initialize()
		{
			SelectionChanged += OnSelectionChanged;
		}


		private ICommand _itemClickCommand;
		//TODO: Remove this as it doesn't exist on Windows
		public ICommand ItemClickCommand
		{
			get { return _itemClickCommand; }
			set
			{
				_itemClickCommand = value;
				OnItemClickCommandChangedPartial(value);
			}
		}

		partial void OnItemClickCommandChangedPartial(ICommand value);

		//Note: by a hackishly convenient coincidence, the binding u:ListViewBaseCommand.Command="{Binding [DoSomething]}" will bind to this property on Uno,
		// because the xaml source generation fails to find ListViewBaseCommand and ignores that part entirely. So...when this property is removed, port ListViewBaseCommand
		// to Xamarin, and all will be well.
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

		public IList<object> SelectedItems { get; }

		internal (int index, SelectorItem container, object item) FocusedIndexContainerItem { get; set; } = (-1, null, null);

		/// <summary>
		/// Marks the starting index of a selection when shift is held and (a pointer is clicked
		/// or Down/Up keys are pressed)
		/// </summary>
		internal int ExtendedShiftSelectionStart { get; set; } = -1;

		internal bool ShouldShowHeader => Header != null || HeaderTemplate != null;
		internal bool ShouldShowFooter => Footer != null || FooterTemplate != null;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			OnApplyTemplatePartial();
		}
		partial void OnApplyTemplatePartial();

		partial void OnSelectionModeChangedPartial(ListViewSelectionMode oldSelectionMode, ListViewSelectionMode newSelectionMode)
		{
			SelectedIndex = -1;
			ExtendedShiftSelectionStart = -1;
			FocusedIndexContainerItem = (-1, null, null);

			foreach (var item in SelectedItems.ToList())
			{
				SetSelectedState(IndexFromItem(item), false);
			}
			SelectedItems.Clear();

			foreach (var item in GetItemsPanelChildren().OfType<SelectorItem>())
			{
				item.UpdateMultiSelectStates(useTransitions: item.IsLoaded);
			}

			ApplyMultiSelectStateToCachedItems();
		}

		partial void ApplyMultiSelectStateToCachedItems();

		internal override void OnItemClicked(int clickedIndex, VirtualKeyModifiers modifiers)
		{
			// Note: don't call base.OnItemClicked(), because we override the default single-selection-only handling

			var clickedItem = ItemFromIndex(clickedIndex);
			var clickedContainer = ContainerFromIndex(clickedIndex) as SelectorItem;

			var (focusedIndex, focusedContainer, focusedItem) = FocusedIndexContainerItem;
			FocusedIndexContainerItem = (clickedIndex, clickedContainer, clickedItem);

			if (IsItemClickEnabled)
			{
				// This is required for the NavigationView which references a non-public issue (#17546992 in NavigationViewList)
				IsItemItsOwnContainerOverride(clickedItem);

				ItemClickCommand.ExecuteIfPossible(clickedItem);
				ItemClick?.Invoke(this, new ItemClickEventArgs { ClickedItem = clickedItem });
			}

			//Handle selection
			switch (SelectionMode)
			{
				case ListViewSelectionMode.None:
					break;
				case ListViewSelectionMode.Single:
					SingleSelectionCase();
					break;
				case ListViewSelectionMode.Multiple:
					MultipleSelectionCase();
					break;
				case ListViewSelectionMode.Extended:
					ExtendedSelectionCase();
					break;
			}

			void SingleSelectionCase()
			{

				if (ItemsSource is ICollectionView collectionView)
				{
					//NOTE: Windows seems to call MoveCurrentTo(item); we set position instead to have expected behavior when you have duplicate items in the list.
					collectionView.MoveCurrentToPosition(clickedIndex);

					// The CollectionView may have intercepted the change
					clickedIndex = collectionView.CurrentPosition;
				}

				if (modifiers.HasFlag(VirtualKeyModifiers.Control) && clickedIndex == SelectedIndex)
				{
					SelectedIndex = -1;
				}
				else
				{
					SelectedIndex = clickedIndex;
				}
			}

			void MultipleSelectionCase()
			{

				if (!modifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					FlipSelectionInMultipleSelection(clickedItem, clickedContainer);
					return;
				}

				// if shift is held, all items between the clicked item and the previously
				// focused item match the selection of the previously focused item
				var newSelection = IsSelected(focusedContainer, focusedItem);

				var multipleLowerBound = Math.Min(focusedIndex, clickedIndex);
				var multipleUpperBound = Math.Max(focusedIndex, clickedIndex);

				for (var currentIndex = multipleLowerBound; currentIndex <= multipleUpperBound; currentIndex++)
				{
					var currentItem = ItemFromIndex(currentIndex);
					var currentContainer = ContainerFromItem(currentItem) as SelectorItem;

					if (IsSelected(currentContainer, currentItem) != newSelection)
					{
						FlipSelectionInMultipleSelection(currentItem, currentContainer);
					}
				}
			}

			void ExtendedSelectionCase()
			{

				if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					// if shift is held, all items between the clicked item and the previously
					// focused item are selected. Everything else is unselected.
					var extendedLowerBound = Math.Min(clickedIndex, ExtendedShiftSelectionStart);
					var extendedUpperBound = Math.Max(clickedIndex, ExtendedShiftSelectionStart);

					for (var currentIndex = 0; currentIndex < Items.Count; currentIndex++)
					{
						var currentItem = ItemFromIndex(currentIndex);
						var currentContainer = ContainerFromItem(currentItem) as SelectorItem;

						if (currentIndex > extendedUpperBound || currentIndex < extendedLowerBound)
						{
							UnselectInMultipleSelection(currentItem, currentContainer);
						}
						else
						{
							SelectInMultipleSelection(currentItem, currentContainer);
						}
					}

					return;
				}

				// any click but a shift-click marks the start of a new selection
				// even if the click actually unselects an item
				ExtendedShiftSelectionStart = clickedIndex;

				if (modifiers.HasFlag(VirtualKeyModifiers.Control))
				{
					FlipSelectionInMultipleSelection(clickedItem, clickedContainer);
					return;
				}

				// no modifiers
				// Act like single selection mode i.e. set one, unset everything else
				for (var currentIndex = 0; currentIndex < Items.Count; currentIndex++)
				{
					if (currentIndex != clickedIndex)
					{
						var currentItem = ItemFromIndex(currentIndex);
						var currentContainer = ContainerFromItem(currentItem) as SelectorItem;

						UnselectInMultipleSelection(currentItem, currentContainer);
					}
				}

				SelectInMultipleSelection(clickedItem, clickedContainer);
			}
		}

		private void FlipSelectionInMultipleSelection(object item, SelectorItem container)
		{
			bool wasSelected = SelectedItems.Remove(item);
			if (!wasSelected)
			{
				SelectedItems.Add(item);
			}

			if (container is { })
			{
				container.IsSelected = !wasSelected;
			}
		}

		private void SelectInMultipleSelection(object item, SelectorItem container)
		{
			if (!SelectedItems.Contains(item))
			{
				SelectedItems.Add(item);
				if (container is { } selectorItem)
				{
					selectorItem.IsSelected = true;
				}
			}
		}

		private void UnselectInMultipleSelection(object item, SelectorItem container)
		{
			if (SelectedItems.Remove(item))
			{
				if (container is { } selectorItem)
				{
					selectorItem.IsSelected = false;
				}
			}
		}

		private void SetSelectedState(int clickedIndex, bool selected)
		{
			if (ContainerFromIndex(clickedIndex) is SelectorItem selectorItem)
			{
				selectorItem.IsSelected = selected;
			}
		}

		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			// Propagate the DataContext manually, since ItemsPanelRoot isn't really part of the visual tree
			ItemsPanelRoot?.SetValue(DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Inheritance);

			OnDataContextChangedPartial();
		}

		partial void OnDataContextChangedPartial();

		internal object ResolveFooterContext()
		{
			return ResolveHeaderOrFooterContext(FooterProperty, FooterTemplateProperty);
		}

		internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args, int section)
		{
			if (RefreshOnCollectionChanged)
			{
				completeRefresh();
				return;
			}

			// https://blog.stephencleary.com/2009/07/interpreting-notifycollectionchangedeve.html
			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (AreEmptyGroupsHidden && (sender as IEnumerable).Count() == args.NewItems.Count)
					{
						//If HidesIfEmpty is true and a group becomes non-empty it is 'new' from the view of UICollectionView and we need to reset state
						// TODO: This could call AddGroup now that it's implemented
						completeRefresh();
						return;
					}
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Inserting {args.NewItems.Count} items starting at {args.NewStartingIndex}");
					}

					// Because new items are added, the containers for existing items with higher indices
					// will be moved, and we must make sure to increase their indices
					SaveContainersBeforeAddForIndexRepair(args.NewItems, args.NewStartingIndex, args.NewItems.Count);
					AddItems(args.NewStartingIndex, args.NewItems.Count, section);
					RepairIndices();

					break;
				case NotifyCollectionChangedAction.Remove:
					if (AreEmptyGroupsHidden && (sender as IEnumerable).None())
					{
						//If HidesIfEmpty is true and a group becomes empty it is 'vanished' from the view of UICollectionView and we need to reset state
						// TODO: This could call RemoveGroup now that it's implemented
						completeRefresh();
						return;
					}
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Deleting {args.OldItems.Count} items starting at {args.OldStartingIndex}");
					}

					SaveContainersBeforeRemoveForIndexRepair(args.OldStartingIndex, args.OldItems.Count);
					var removedContainers = CaptureContainers(args.OldStartingIndex, args.OldItems.Count);
					RemoveItems(args.OldStartingIndex, args.OldItems.Count, section);
					RepairIndices();
					CleanUpContainers(removedContainers);

					break;
				case NotifyCollectionChangedAction.Replace:
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Replacing {args.NewItems.Count} items starting at {args.NewStartingIndex}");
					}

					ReplaceItems(args.NewStartingIndex, args.NewItems.Count, section);

					break;
				case NotifyCollectionChangedAction.Move:
					// TODO PBI #19974: Fully implement NotifyCollectionChangedActions and map them to the appropriate calls
					// on UICollectionView, MoveItems
					Refresh();
					break;
				case NotifyCollectionChangedAction.Reset:
					CleanUpAllContainers();
					Refresh();
					break;
			}

			//Call base after so that list state is 'fresh' when we update SelectedItem
			base.OnItemsSourceSingleCollectionChanged(sender, args, section);

			void completeRefresh()
			{
				Refresh();
				ObserveCollectionChanged();
				base.OnItemsSourceSingleCollectionChanged(sender, args, section);
			}
		}

		/// <summary>
		/// Stores materialized containers starting a given index, so that their
		/// ItemsControl.IndexForContainerProperty can be updated after the collection changes.		
		/// </summary>
		/// <param name="startingIndex">The minimum index of containers we care about.</param>
		/// <param name="indexChange">How does the index change.</param>
		private void SaveContainersBeforeAddForIndexRepair(IList addedItems, int startingIndex, int indexChange)
		{
			_containersForIndexRepair.Clear();
			foreach (var container in MaterializedContainers)
			{
				var currentIndex = (int)container.GetValue(ItemsControl.IndexForItemContainerProperty);
				if (currentIndex is -1 && container is ContentControl ctrl)
				{
					var offset = addedItems.IndexOf(ctrl.Content);
					if (offset >= 0)
					{
						_containersForIndexRepair.Add(container, startingIndex + offset);
					}
				}
				else if (currentIndex >= startingIndex)
				{
					// we store the index, that should be set after the collection change
					_containersForIndexRepair.Add(container, currentIndex + indexChange);
				}
			}
		}

		/// <summary>
		/// Stores materialized containers starting a given index, so that their
		/// ItemsControl.IndexForContainerProperty can be updated after the collection changes.		
		/// </summary>
		/// <param name="startingIndex">The minimum index of containers we care about.</param>
		/// <param name="indexChange">How does the index change.</param>
		private void SaveContainersBeforeRemoveForIndexRepair(int startingIndex, int indexChange)
		{
			_containersForIndexRepair.Clear();

			var firstRemainingIndex = startingIndex + indexChange;
			foreach (var container in MaterializedContainers)
			{
				var currentIndex = (int)container.GetValue(ItemsControl.IndexForItemContainerProperty);
				if (currentIndex < startingIndex)
				{
					continue;
				}

				if (currentIndex < firstRemainingIndex)
				{
					_containersForIndexRepair.Add(container, -1);
				}
				else
				{
					// we store the index, that should be set after the collection change
					_containersForIndexRepair.Add(container, currentIndex - indexChange);
				}
			}
		}

		/// <summary>
		/// Sets the indices of stored materialized containers to the appropriate index after
		/// collection change.
		/// </summary>
		private void RepairIndices()
		{
			foreach (var containerPair in _containersForIndexRepair)
			{
				containerPair.Key.SetValue(ItemsControl.IndexForItemContainerProperty, containerPair.Value);
			}
			_containersForIndexRepair.Clear();
		}

		private void CleanUpAllContainers()
		{
			if (ShouldItemsControlManageChildren)
			{
				return;
			}

			foreach (var container in MaterializedContainers)
			{
				CleanUpContainer(container);
			}
			ItemsPanelRoot?.Children?.Clear();
		}

		private ICollection<DependencyObject> CaptureContainers(int startingIndex, int length)
		{
			if (ShouldItemsControlManageChildren)
			{
				return Array.Empty<DependencyObject>();
			}

			var containers = new List<DependencyObject>(length);
			foreach (var container in MaterializedContainers)
			{
				var index = (int)container.GetValue(ItemsControl.IndexForItemContainerProperty);
				if (startingIndex <= index && index < startingIndex + length)
				{
					containers.Add(container);
				}
			}
			return containers;
		}

		private void CleanUpContainers(ICollection<DependencyObject> containersToCleanup)
		{
			foreach (var container in containersToCleanup)
			{
				CleanUpContainer(container);
			}
		}

		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);

			InitializeDataSourceSelectionInfo();
		}

		internal override void OnItemsSourceGroupsChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (RefreshOnCollectionChanged)
			{
				Refresh();
				ObserveCollectionChanged();
				base.OnItemsSourceGroupsChanged(sender, args);
				return;
			}

			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					//Add group header(s), group items
					for (int i = args.NewStartingIndex; i < args.NewStartingIndex + args.NewItems.Count; i++)
					{
						// On Android we add all items before any group headers; otherwise, since group headers are 'after' all items in the indexing 
						// used by the RecyclerView adapter, the additions will not be registered correctly.
						AddGroupItems(i);
					}
					for (int i = args.NewStartingIndex; i < args.NewStartingIndex + args.NewItems.Count; i++)
					{
						// Notify add of groups that are visible to the native list
						if (!AreEmptyGroupsHidden || GetGroupCount(i) > 0)
						{
							var adjustedIndex = AdjustGroupIndexForHidesIfEmpty(i);
							AddGroup(adjustedIndex);
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					// For Android, remove old group items and add new group items
					for (int i = args.OldItems.Count - 1; i >= 0; i--)
					{
						RemoveGroupItems(args.OldStartingIndex + i, GetCachedGroupCount(args.OldStartingIndex + i));
					}
					for (int i = args.OldItems.Count - 1; i >= 0; i--)
					{
						// Notify remove of groups that are visible to the native list
						if (!AreEmptyGroupsHidden || GetCachedGroupCount(args.OldStartingIndex + i) > 0)
						{
							var adjustedIndex = AdjustGroupIndexForHidesIfEmpty(args.OldStartingIndex + i);
							RemoveGroup(adjustedIndex);
						}
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					// For Android, remove old group items and add new group items
					for (int i = args.OldItems.Count - 1; i >= 0; i--)
					{
						RemoveGroupItems(args.OldStartingIndex + i, GetCachedGroupCount(args.OldStartingIndex + i));
					}
					for (int i = args.NewStartingIndex; i < args.NewStartingIndex + args.NewItems.Count; i++)
					{
						AddGroupItems(i);
					}

					for (int i = args.NewStartingIndex; i < args.NewStartingIndex + args.NewItems.Count; i++)
					{
						var adjustedIndex = AdjustGroupIndexForHidesIfEmpty(i);
						ReplaceGroup(adjustedIndex);
					}
					break;
				default:
					//TODO: handle Move
					Refresh();
					break;
			}

			ObserveCollectionChanged();

			base.OnItemsSourceGroupsChanged(sender, args);
		}

		/// <summary>
		/// Update onscreen group header DataContext when ICollectionViewGroup.Group property changes.
		/// </summary>
		internal override void OnGroupPropertyChanged(ICollectionViewGroup group, int groupIndex)
		{
			base.OnGroupPropertyChanged(group, groupIndex);

			var groupContainer = ContainerFromGroupIndex(groupIndex);

			if (groupContainer != null)
			{
				groupContainer.DataContext = group.Group;
			}
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			// Index will be repaired by virtue of ItemsControl
			_containersForIndexRepair.Remove(element);

			base.PrepareContainerForItemOverride(element, item);

			if (element is SelectorItem selectorItem)
			{
				selectorItem.UpdateMultiSelectStates(useTransitions: selectorItem.IsLoaded);
			}
		}

		internal override void ContainerPreparedForItem(object item, SelectorItem itemContainer, int itemIndex)
		{
			base.ContainerPreparedForItem(item, itemContainer, itemIndex);

			PrepareContainerForDragDrop(itemContainer);

			ContainerContentChanging?.Invoke(this, new ContainerContentChangingEventArgs(item, itemContainer, itemIndex));
		}

		internal override void ContainerClearedForItem(object item, SelectorItem itemContainer)
		{
			ClearContainerForDragDrop(itemContainer);

			base.ContainerClearedForItem(item, itemContainer);
		}

		/// <summary>
		/// Insert items in a newly inserted group. This is only needed on Android, which doesn't natively support the concept of a group.
		/// </summary>
		partial void AddGroupItems(int groupIndex);

		/// <summary>
		/// Remove items in a disappearing group. This is only needed on Android, which doesn't natively support the concept of a group.
		/// </summary>
		partial void RemoveGroupItems(int groupIndex, int groupCount);

		/// <summary>
		/// Carry out collection Replace action. We just rebind the existing item, rather than calling the native replace method, to minimize 'flicker' when the item is nearly the same. 
		/// </summary>
		private void ReplaceItems(int firstItem, int count, int section)
		{
			for (int i = 0; i < count; i++)
			{
				var unoIndexPath = Uno.UI.IndexPath.FromRowSection(firstItem + i, section);
				var flatIndex = GetIndexFromIndexPath(unoIndexPath);
				var container = ContainerFromIndex(flatIndex);
				if (container != null)
				{
#if __APPLE_UIKIT__ || __ANDROID__ // TODO: The managed ListView should similarly go through the recycling to use the proper container matching the new template
					var item = GetDisplayItemFromIndexPath(unoIndexPath);
					if (HasTemplateChanged(((FrameworkElement)container).DataContext, item))
					{
						// If items are using different templates, we should go through the native replace operation, to use a container
						// with the right template.
						NativeReplaceItems(i, 1, section);
					}
					else
#endif
					{
						PrepareContainerForIndex(container, flatIndex);
					}
				}
				else
				{
					// On Android, if we can't find a materialized view to rebind, we call the native replace-equivalent to make sure that
					// views awaiting recycling are correctly marked as needing rebinding.
					NativeReplaceItems(i, 1, section);
				}
			}
		}

#if __APPLE_UIKIT__ || __ANDROID__
		/// <summary>
		/// Does <paramref name="newItem"/> resolve to a different template than <paramref name="oldItem"/>?
		/// </summary>
		private bool HasTemplateChanged(object oldItem, object newItem)
		{
			if (ItemTemplateSelector == null)
			{
				return false;
			}

			return ResolveItemTemplate(oldItem) != ResolveItemTemplate(newItem);
		}
#endif

		partial void NativeReplaceItems(int firstItem, int count, int section);

		internal object ResolveHeaderContext()
		{
			return ResolveHeaderOrFooterContext(HeaderProperty, HeaderTemplateProperty);
		}

		/// <summary>
		/// Resolve the context to use for header/footer. This should be the Header/Footer properties if they are set;
		/// if not, but the HeaderTemplate/FooterTemplate is non-null, then the ListViewBase's DataContext should be used.
		/// </summary>
		private object ResolveHeaderOrFooterContext(DependencyProperty contextProperty, DependencyProperty templateProperty)
		{
			if (this.IsDependencyPropertySet(contextProperty))
			{
				return GetValue(contextProperty);
			}
			else if (GetValue(templateProperty) != null)
			{
				return DataContext;
			}
			else
			{
				return null;
			}
		}

		internal override bool IsSelected(int index)
		{
			if (SelectionMode == ListViewSelectionMode.None)
			{
				return false;
			}
			else if (DataSourceAsSelectionInfo is { } info)
			{
				return info.IsSelected(index);
			}
			else if (SelectionMode == ListViewSelectionMode.Single)
			{
				return index == SelectedIndex;
			}
			else if (SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended)
			{
				return SelectedItems.Any(item => IndexFromItem(item).Equals(index));
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Try to fetch more items, if the ItemsSource supports <see cref="ISupportIncrementalLoading"/>.
		/// </summary>
		/// <param name="currentLastItem">The last item index currently visible. (In practice this may also be less than the last visible index, without ill effects.)</param>
		internal void TryLoadMoreItems(int currentLastItem)
		{
			if (CanLoadItems)
			{
				// IncrementalLoadingThreshold = 0 means 'load when we have less than a page of items left to show'
				var desiredItemBuffer = (IncrementalLoadingThreshold + 1) * PageSize;
				var remainingItems = NumberOfItems - 1 - currentLastItem;
				if (remainingItems <= desiredItemBuffer)
				{
					var pageSize = Math.Max(1, PageSize); //TODO: PageSize should probably report how many items *could* fit on the page (based on item extent /viewport height), not how many actually do
					TryLoadMoreItemsInner((int)(DataFetchSize * pageSize));
				}
			}
		}

		private void TryLoadFirstItem()
		{
			if (CanLoadItems)
			{
				TryLoadMoreItemsInner(1);
			}
		}

		/// <summary>
		/// True if the source is an <see cref="ISupportIncrementalLoading"/> with more items, incremental loading is enabled, and no request is pending.
		/// </summary>
		private bool CanLoadItems => !_isIncrementalLoadingInFlight
			&& IncrementalLoadingTrigger == IncrementalLoadingTrigger.Edge
			&& SourceHasMoreItems;

		private bool SourceHasMoreItems => (GetItems() is ISupportIncrementalLoading incrementalSource && incrementalSource.HasMoreItems) ||
			(GetItems() is ICollectionView collectionView && collectionView.HasMoreItems);

		private void TryLoadMoreItemsInner(int count)
		{
			_isIncrementalLoadingInFlight = true;
			_ = LoadMoreItemsAsync(GetItems(), (uint)count);
		}

		private async Task LoadMoreItemsAsync(object incrementalSource, uint count)
		{
			LoadMoreItemsResult result = default(LoadMoreItemsResult);
			try
			{
				if (incrementalSource is ISupportIncrementalLoading supportIncrementalLoading)
				{
					result = await supportIncrementalLoading.LoadMoreItemsAsync(count);
				}
				else if (incrementalSource is ICollectionView collectionView)
				{
					result = await collectionView.LoadMoreItemsAsync(count);
				}
				else
				{
					throw new InvalidOperationException();
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn($"{nameof(LoadMoreItemsAsync)} failed.", e);
				}
			}
			finally
			{
				_isIncrementalLoadingInFlight = false;
			}

			// If we got some items, try to get some more if we haven't filled the desired buffer. This is mainly needed to handle an 
			// unfilled viewport because PageSize doesn't return the 'potential' number of visible items.
			if (result.Count > 0)
			{
				TryLoadMoreItems();
			}
		}
	}
}
