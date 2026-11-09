using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Helpers;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation.Collections;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class Selector : ItemsControl
	{
		public event SelectionChangedEventHandler SelectionChanged;

		/// <summary>
		/// Templates for which it's known that the template root doesn't qualify as a container.
		/// </summary>
		private readonly HashSet<DataTemplate> _itemTemplatesThatArentContainers = new HashSet<DataTemplate>();

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(
				"SelectedItem",
				typeof(object),
				typeof(Selector),
				new FrameworkPropertyMetadata(defaultValue: null));

		public object SelectedItem
		{
			get => this.GetValue(SelectedItemProperty);
			set => this.SetValue(SelectedItemProperty, value);
		}

		public int SelectedIndex
		{
			get => (int)this.GetValue(SelectedIndexProperty);
			set => this.SetValue(SelectedIndexProperty, Boxer.Box(value));
		}

		public static DependencyProperty SelectedIndexProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedIndex),
				typeof(int),
				typeof(Selector),
				new FrameworkPropertyMetadata(IntBoxes.NegativeOne, coerceValueCallback: CoerceSelectedIndex));

		// Explicitly setting -1 drops a pending index even when SelectedIndex already is -1. Uno raises no
		// property change for that set, so OnSelectedIndexChanged cannot do it.
		private static object CoerceSelectedIndex(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
		{
			if (dependencyObject is Selector selector &&
				baseValue is -1 &&
				selector.IsSelectionReentrancyAllowed() &&
				!selector.IsInit())
			{
				selector.m_selectedIndexValueSetBeforeItemsAvailable = -1;
			}

			return baseValue;
		}

		public string SelectedValuePath
		{
			get => (string)this.GetValue(SelectedValuePathProperty);
			set => this.SetValue(SelectedValuePathProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty SelectedValuePathProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				name: nameof(SelectedValuePath),
				propertyType: typeof(string),
				ownerType: typeof(Selector),
				typeMetadata: new FrameworkPropertyMetadata(""));

		public object SelectedValue
		{
			get => this.GetValue(SelectedValueProperty);
			set => this.SetValue(SelectedValueProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty SelectedValueProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				name: nameof(SelectedValue),
				propertyType: typeof(object),
				ownerType: typeof(Selector),
				typeMetadata: new FrameworkPropertyMetadata(defaultValue: null));

		public bool? IsSynchronizedWithCurrentItem
		{
			get => (bool?)GetValue(IsSynchronizedWithCurrentItemProperty);
			set => SetValue(IsSynchronizedWithCurrentItemProperty, value);
		}

		public static DependencyProperty IsSynchronizedWithCurrentItemProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(IsSynchronizedWithCurrentItem),
				typeof(bool?),
				typeof(Selector),
				new FrameworkPropertyMetadata(default(bool?)));

		public bool IsSelectionActive
		{
			get => (bool)GetValue(IsSelectionActiveProperty);
			set => SetValue(IsSelectionActiveProperty, value);
		}

		internal static DependencyProperty IsSelectionActiveProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSelectionActive),
				typeof(bool),
				typeof(Selector),
				new FrameworkPropertyMetadata(BoolBoxes.False));

		internal ISelectionInfo DataSourceAsSelectionInfo
		{
			get => m_tpDataSourceAsSelectionInfo;
			set => m_tpDataSourceAsSelectionInfo = value;
		}

		// Whether or not this Selector allows multiple selection
		internal virtual bool CanSelectMultiple => false;

		/// <summary>
		/// This is always true for <see cref="FlipView"/> and <see cref="ComboBox"/>, and depends on the value of <see cref="ListViewBase.SelectionMode"/> for <see cref="ListViewBase"/>.
		/// </summary>
		internal virtual bool IsSingleSelection => true;

		internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs c, int section)
		{
			var isItemsHostNotified = false;

			void NotifyItemsHost()
			{
				if (!isItemsHostNotified)
				{
					isItemsHostNotified = true;
					base.OnItemsSourceSingleCollectionChanged(sender, c, section);
				}
			}

			var changes = ToVectorChanges(c, section);

			ProcessItemsChanges(changes, NotifyItemsHost);
		}

		// Flat vector changes for a collection change of one section, as WinUI's ItemCollection raises them.
		internal List<IVectorChangedEventArgs> ToVectorChanges(NotifyCollectionChangedEventArgs c, int section)
		{
			int ToFlatIndex(int indexInSection) =>
				IsGrouping ? GetIndexFromIndexPath(Uno.UI.IndexPath.FromRowSection(indexInSection, section)) : indexInSection;

			List<IVectorChangedEventArgs> changes = new();
			switch (c.Action)
			{
				case NotifyCollectionChangedAction.Add when c.NewStartingIndex >= 0:
					for (var i = 0; i < c.NewItems.Count; i++)
					{
						changes.Add(new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)ToFlatIndex(c.NewStartingIndex + i)));
					}
					break;
				case NotifyCollectionChangedAction.Remove when c.OldStartingIndex >= 0:
					for (var i = 0; i < c.OldItems.Count; i++)
					{
						changes.Add(new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)ToFlatIndex(c.OldStartingIndex)));
					}
					break;
				case NotifyCollectionChangedAction.Replace when c.NewStartingIndex >= 0:
					for (var i = 0; i < c.NewItems.Count; i++)
					{
						changes.Add(new VectorChangedEventArgs(CollectionChange.ItemChanged, (uint)ToFlatIndex(c.NewStartingIndex + i)));
					}
					break;
				// MUX Reference BindableObservableVectorWrapper.cpp, ProcessCollectionChange: Move is raised as a Reset.
				default:
					changes.Add(new VectorChangedEventArgs(CollectionChange.Reset, 0));
					break;
			}

			return changes;
		}

		internal override void OnItemsSourceGroupsChanged(object sender, NotifyCollectionChangedEventArgs c)
		{
			var isItemsHostNotified = false;
			List<IVectorChangedEventArgs> changes = new();

			void NotifyItemsHost()
			{
				if (!isItemsHostNotified)
				{
					isItemsHostNotified = true;
					base.OnItemsSourceGroupsChanged(sender, c);

					if (CustomItemsHost is ModernCollectionBasePanel panel)
					{
						NotifyPanelOfGroupChange(panel);
					}
				}
			}

			// MUX Reference GroupedDataCollectionView_Partial.cpp, ProcessGroupChange and NotifyOfGroupChange:
			// a removed group raises its items, high to low, ahead of the group. An added group raises them after it.
			void NotifyPanelOfGroupChange(ModernCollectionBasePanel panel)
			{
				void NotifyGroup(CollectionChange change, int index) =>
					panel.NotifyOfGroupsChanged(null!, new VectorChangedEventArgs(change, (uint)index));

				void NotifyItems(CollectionChange change, int groupIndex, int count)
				{
					var firstItem = FirstFlatIndexOfGroup(groupIndex);
					for (var i = count - 1; i >= 0; i--)
					{
						NotifyCustomItemsHost(panel, new VectorChangedEventArgs(change, (uint)(firstItem + i)));
					}
				}

				static int GroupSize(object group) => group is ICollectionViewGroup { GroupItems: { } items } ? items.Count : 0;

				if (changes is [{ CollectionChange: CollectionChange.Reset }])
				{
					NotifyGroup(CollectionChange.Reset, 0);
					NotifyCustomItemsHost(panel, new VectorChangedEventArgs(CollectionChange.Reset, 0));
					return;
				}

				switch (c.Action)
				{
					case NotifyCollectionChangedAction.Add:
						for (var i = 0; i < c.NewItems.Count; i++)
						{
							NotifyGroup(CollectionChange.ItemInserted, c.NewStartingIndex + i);
							NotifyItems(CollectionChange.ItemInserted, c.NewStartingIndex + i, GetGroupCount(c.NewStartingIndex + i));
						}
						break;

					case NotifyCollectionChangedAction.Remove:
						for (var i = c.OldItems.Count - 1; i >= 0; i--)
						{
							NotifyItems(CollectionChange.ItemRemoved, c.OldStartingIndex + i, GroupSize(c.OldItems[i]));
							NotifyGroup(CollectionChange.ItemRemoved, c.OldStartingIndex + i);
						}
						break;

					case NotifyCollectionChangedAction.Replace:
						for (var i = 0; i < c.NewItems.Count; i++)
						{
							NotifyItems(CollectionChange.ItemRemoved, c.NewStartingIndex + i, GroupSize(c.OldItems[i]));
							NotifyGroup(CollectionChange.ItemChanged, c.NewStartingIndex + i);
							NotifyItems(CollectionChange.ItemInserted, c.NewStartingIndex + i, GetGroupCount(c.NewStartingIndex + i));
						}
						break;
				}
			}

			int FirstFlatIndexOfGroup(int groupIndex)
			{
				var flatIndex = 0;
				for (var i = 0; i < groupIndex; i++)
				{
					flatIndex += GetGroupCount(i);
				}
				return flatIndex;
			}

			bool TryAddRemovedGroups(List<IVectorChangedEventArgs> target)
			{
				var flatIndex = FirstFlatIndexOfGroup(c.OldStartingIndex);
				foreach (var group in c.OldItems)
				{
					if (group is not ICollectionViewGroup { GroupItems: { } groupItems })
					{
						return false;
					}

					for (var i = 0; i < groupItems.Count; i++)
					{
						target.Add(new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)flatIndex));
					}
				}
				return true;
			}

			void AddInsertedGroups(List<IVectorChangedEventArgs> target)
			{
				var flatIndex = FirstFlatIndexOfGroup(c.NewStartingIndex);
				for (var groupIndex = c.NewStartingIndex; groupIndex < c.NewStartingIndex + c.NewItems.Count; groupIndex++)
				{
					for (var i = 0; i < GetGroupCount(groupIndex); i++)
					{
						target.Add(new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)flatIndex++));
					}
				}
			}

			// The items of a group move with it, while WinUI's grouped view raises one flat change per item.
			var isReset = false;
			switch (c.Action)
			{
				case NotifyCollectionChangedAction.Add when c.NewStartingIndex >= 0:
					AddInsertedGroups(changes);
					break;
				case NotifyCollectionChangedAction.Remove when c.OldStartingIndex >= 0:
					isReset = !TryAddRemovedGroups(changes);
					break;
				case NotifyCollectionChangedAction.Replace when c.NewStartingIndex >= 0 && c.OldStartingIndex >= 0:
					isReset = !TryAddRemovedGroups(changes);
					if (!isReset)
					{
						AddInsertedGroups(changes);
					}
					break;
				default:
					isReset = true;
					break;
			}

			if (isReset)
			{
				changes.Clear();
				changes.Add(new VectorChangedEventArgs(CollectionChange.Reset, 0));
			}

			ProcessItemsChanges(changes, NotifyItemsHost);
		}

		// TODO Uno: WinUI recycles during the reset, where the focused index ends up at -1 and focus goes back to the list.
		// Here the refresh comes after it and drops focus, and an item can only take focus again once it's loaded.
		private protected override void OnFocusedContainerRefreshed(FocusState focusState)
		{
			SetFocusedIndex(-1);
			SetLastFocusedIndex(0);

			_ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (IsLoaded && Uno.UI.Xaml.Core.VisualTree.GetFocusManagerForElement(this)?.FocusedElement is null)
				{
					SetFocusedItem(-1, false /*shouldScrollIntoView*/, true /*forceFocus*/, focusState);
				}
			});
		}

		// Stands in for the NotifyOfSourceChanged and OnItemsChanged calls that WinUI's ItemCollection makes for each change.
		private void ProcessItemsChanges(List<IVectorChangedEventArgs> changes, Action notifyItemsHost)
		{
			foreach (var change in changes)
			{
				NotifyOfSourceChanged(change, notifyItemsHost);
				OnItemsChanged(change, notifyItemsHost);
			}

			notifyItemsHost();
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

		private protected override void Refresh()
		{
			base.Refresh();
			_itemTemplatesThatArentContainers.Clear();
			RefreshPartial();
		}

		partial void RefreshPartial();

		/// <summary>
		/// Is the managed implementation for virtualized lists (ItemsStackPanel, ItemsWrapGrid, CarouselPanel etc) used on this platform?
		/// </summary>
		internal const bool UsesManagedLayouting =
			true;

#if HAS_UNO
		// Forwarders for callers of the former Selector, which predates the port.
		// TODO Uno: remove what the ComboBox, FlipView and ListViewBase ports stop calling.

		private protected int m_iFocusedIndex => GetFocusedIndex();

		// Get the ItemsHost's orientations. FlipView overrides it to report its fixed orientation.
		protected virtual (Orientation PhysicalOrientation, Orientation LogicalOrientation) GetItemsHostOrientations()
		{
			GetPanelOrientations(ItemsPanelRoot, out var physicalOrientation, out var logicalOrientation);

			return (physicalOrientation, logicalOrientation);
		}

		// Called on every change of the property, reentrant ones included: ComboBox has to update its content presenter.
		internal virtual void OnSelectedItemChanged(object oldSelectedItem, object selectedItem, bool updateItemSelectedState) =>
			OnSelectedItemChanged(oldSelectedItem, selectedItem);

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

		internal virtual bool IsSelected(int index) => IsIndexSelected(index);

		internal void OnItemClicked(SelectorItem selectorItem, VirtualKeyModifiers modifiers) => OnItemClicked(IndexFromContainer(selectorItem), modifiers);

		internal virtual void OnItemClicked(int clickedIndex, VirtualKeyModifiers modifiers) => SelectedIndex = clickedIndex;

		// SelectorItem's caller.
		internal void NotifyListItemSelected(SelectorItem pSelectorItem, bool oldValue, bool bIsSelected) =>
			NotifyListItemSelected(pSelectorItem, bIsSelected);

		internal void MakeSingleSelection(int index, bool animateIfBringIntoView, object selectedItem, FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None) =>
			MakeSingleSelection((uint)index, animateIfBringIntoView, selectedItem, focusNavigationDirection);

		internal void AutomationPeerAddToSelection(int index, object item)
		{
			AutomationPeerAddToSelection((uint)index, item);
		}

		internal void AutomationPeerRemoveFromSelection(int index, object item)
		{
			AutomationPeerRemoveFromSelection((uint)index, item);
		}

		internal bool AutomationPeerIsSelected(int index, object item) => index >= 0 ? IsSelected(index) : AutomationPeerIsSelected(item);
#endif
	}
}
