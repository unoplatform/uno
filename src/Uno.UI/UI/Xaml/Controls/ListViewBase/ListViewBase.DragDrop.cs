#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI;
using _DragEventArgs = global::Windows.UI.Xaml.DragEventArgs;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Windows.UI.Xaml.Controls
{
	partial class ListViewBase
	{
		private const string ReorderOwnerFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__source__";
		private const string ReorderItemFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__source__item__";
		private const string ReorderContainerFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__source__container__";
		private const string DragItemsFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__items__";
		internal bool IsCustomReorder;

		public event DragItemsStartingEventHandler DragItemsStarting;
		public event TypedEventHandler<ListViewBase, DragItemsCompletedEventArgs> DragItemsCompleted;

#pragma warning disable CS0414 // Field currently used only on Android.
		private bool _isProcessingReorder;
#pragma warning restore CS0414

		#region CanReorderItems (DP)
		public static DependencyProperty CanReorderItemsProperty { get; } = DependencyProperty.Register(
			nameof(CanReorderItems),
			typeof(bool),
			typeof(ListViewBase),
			new FrameworkPropertyMetadata(default(bool)));

		public bool CanReorderItems
		{
			get => (bool)GetValue(CanReorderItemsProperty);
			set => SetValue(CanReorderItemsProperty, value);
		}
		#endregion

		#region CanDragItems (DP)
		public static DependencyProperty CanDragItemsProperty { get; } = DependencyProperty.Register(
			nameof(CanDragItems),
			typeof(bool),
			typeof(ListViewBase),
			new FrameworkPropertyMetadata(default(bool), OnCanDragItemsChanged));

		public bool CanDragItems
		{
			get => (bool)GetValue(CanDragItemsProperty);
			set => SetValue(CanDragItemsProperty, value);
		}

		private static void OnCanDragItemsChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
		{
			if (snd is ListViewBase that && args.NewValue is bool canDragItems)
			{
				var items = that.MaterializedContainers.OfType<UIElement>();
				if (canDragItems)
				{
					items.ForEach(PrepareContainerForDragDropCore);
				}
				else
				{
					items.ForEach(ClearContainerForDragDrop);
				}
			}
		}
		#endregion

		private void PrepareContainerForDragDrop(UIElement itemContainer)
		{
			if (CanDragItems)
			{
				PrepareContainerForDragDropCore(itemContainer);
			}
		}

		private static void PrepareContainerForDragDropCore(UIElement itemContainer)
		{
			if (itemContainer is null) // Even if flagged as impossible by nullable check, https://github.com/unoplatform/uno/issues/4725
			{
				return;
			}

			// Known issue: the ContainerClearedForItem might not be invoked properly for all items on some platforms.
			// This patch is acceptable as event handlers are static (so they won't leak).
			itemContainer.DragStarting -= OnItemContainerDragStarting;

			itemContainer.CanDrag = true;
			itemContainer.DragStarting += OnItemContainerDragStarting;
		}

		private static void ClearContainerForDragDrop(UIElement itemContainer)
		{
			if (itemContainer is null) // Even if flagged as impossible by nullable check, https://github.com/unoplatform/uno/issues/4725
			{
				return;
			}

			itemContainer.CanDrag = false;
			itemContainer.DragStarting -= OnItemContainerDragStarting;

			itemContainer.DragEnter -= OnReorderDragUpdated;
			itemContainer.DragOver -= OnReorderDragUpdated;
			itemContainer.DragLeave -= OnReorderDragLeave;
			itemContainer.Drop -= OnReorderCompleted;
		}

		private static void OnItemContainerDragStarting(UIElement sender, DragStartingEventArgs innerArgs)
		{
			if (ItemsControlFromItemContainer(sender) is ListViewBase that && that.CanDragItems)
			{
				// only raise DragItemsCompleted if DragItemsStarting was raised
				sender.DropCompleted -= OnItemContainerDragCompleted;
				sender.DropCompleted += OnItemContainerDragCompleted;
				// The items contains all selected items ONLY if the draggedItem is selected.
				var draggedItem = that.ItemFromContainer(sender);
				var items =
					draggedItem is null ? new List<object>()
					: (that.SelectionMode == ListViewSelectionMode.Multiple || that.SelectionMode == ListViewSelectionMode.Extended)
						&& that.SelectedItems is { Count: > 0 } selected
						&& selected.Contains(draggedItem)
						? selected.ToList()
						: new List<object>(1) { draggedItem };
				var args = new DragItemsStartingEventArgs(innerArgs, items);

				that.DragItemsStarting?.Invoke(that, args);

				// The application has the ability to add some items in the list, so make sure to freeze it only after event has been raised.
				args.Data.SetData(DragItemsFormatId, args.Items.ToList());

				// The ListView must have both CanReorderItems and AllowDrop flags set to allow re-ordering (UWP)
				// We also do not allow re-ordering if we where not able to find the item (as it has to be hidden in the view) (Uno only)
				if (that.CanReorderItems && that.AllowDrop && draggedItem is { })
				{
					args.Data.SetData(ReorderOwnerFormatId, that);
					args.Data.SetData(ReorderItemFormatId, draggedItem);
					args.Data.SetData(ReorderContainerFormatId, sender);

					// For safety only, avoids double subscription
					that.DragEnter -= OnReorderDragUpdated;
					that.DragOver -= OnReorderDragUpdated;
					that.DragLeave -= OnReorderDragLeave;
					that.Drop -= OnReorderCompleted;

					if (!that.IsCustomReorder)
					{
						that.DragEnter += OnReorderDragUpdated;
						that.DragOver += OnReorderDragUpdated;
						that.DragLeave += OnReorderDragLeave;
						that.Drop += OnReorderCompleted;
					}
					that.m_tpPrimaryDraggedContainer = sender as SelectorItem;

					that.ChangeSelectorItemsVisualState(true);
				}
			}

		}

		private static void OnItemContainerDragCompleted(UIElement sender, DropCompletedEventArgs innerArgs)
		{
			// Note: It's not the responsibility of the ListView to remove item from the source, not matter the AcceptedOperation.

			if (ItemsControlFromItemContainer(sender) is ListViewBase that)
			{
				that.DragEnter -= OnReorderDragUpdated;
				that.DragOver -= OnReorderDragUpdated;
				that.DragLeave -= OnReorderDragLeave;
				that.Drop -= OnReorderCompleted;

				if (that.CanDragItems)
				{
					var items = innerArgs.Info.Data.FindRawData(DragItemsFormatId) as IReadOnlyList<object> ?? new List<object>(0);
					var args = new DragItemsCompletedEventArgs(innerArgs, items);

					that.DragItemsCompleted?.Invoke(that, args);
				}

				// Normally this will have been done by OnReorderCompleted, but sometimes OnReorderCompleted may not be called
				// (eg if drag was released outside bounds of list)
				that.CleanupReordering();
			}
			sender.DropCompleted -= OnItemContainerDragCompleted;
		}

		private static void OnReorderDragUpdated(object sender, _DragEventArgs dragEventArgs)
		{
			var that = sender as ListViewBase;
			var src = dragEventArgs.DataView.FindRawData(ReorderOwnerFormatId) as ListViewBase;
			var item = dragEventArgs.DataView.FindRawData(ReorderItemFormatId);
			var container = dragEventArgs.DataView.FindRawData(ReorderContainerFormatId) as FrameworkElement; // TODO: This might have changed/been recycled if scrolled 
			if (that is null || src is null || item is null || container is null || src != that)
			{
				if (dragEventArgs.Log().IsEnabled(LogLevel.Warning)) dragEventArgs.Log().Warn("Invalid reorder event.");
				dragEventArgs.AcceptedOperation = DataPackageOperation.None;

				return;
			}

			dragEventArgs.AcceptedOperation = DataPackageOperation.Move;
#pragma warning disable CS0162 // Unreachable code since RenderTargetBitmap.IsImplemented is a const
			if (RenderTargetBitmap.IsImplemented)
			{
				dragEventArgs.DragUIOverride.IsGlyphVisible = false;
				dragEventArgs.DragUIOverride.IsCaptionVisible = false;
			}
#pragma warning restore CS0162

			var position = dragEventArgs.GetPosition(that);
			that.UpdateReordering(position, container, item);

			// See what our edge scrolling action should be...
			var panVelocity = that.ComputeEdgeScrollVelocity(position);
			// And request it.
			that.SetPendingAutoPanVelocity(panVelocity);
		}

		private static void OnReorderDragLeave(object sender, _DragEventArgs dragEventArgs)
		{
			var that = sender as ListViewBase;
			var src = dragEventArgs.DataView.FindRawData(ReorderOwnerFormatId) as ListViewBase;
			if (that is null || src != that)
			{
				if (dragEventArgs.Log().IsEnabled(LogLevel.Warning)) dragEventArgs.Log().Warn("Invalid reorder event.");

				return;
			}

			that.CleanupReordering();
			that.SetPendingAutoPanVelocity(PanVelocity.Stationary);
		}

		private static void OnReorderCompleted(object sender, _DragEventArgs dragEventArgs)
		{
			var that = sender as ListViewBase;

			that?.SetPendingAutoPanVelocity(PanVelocity.Stationary);

			var src = dragEventArgs.DataView.FindRawData(ReorderOwnerFormatId) as ListViewBase;
			var item = dragEventArgs.DataView.FindRawData(ReorderItemFormatId);
			var container = dragEventArgs.DataView.FindRawData(ReorderContainerFormatId) as FrameworkElement; // TODO: This might have changed/been recycled if scrolled 
			if (that is null || src is null || item is null || container is null || src != that)
			{
				if (dragEventArgs.Log().IsEnabled(LogLevel.Warning)) dragEventArgs.Log().Warn("Invalid reorder event.");

				return;
			}

			var updatedIndex = that.CompleteReordering(container, item);

			that.m_tpPrimaryDraggedContainer = null;

			that.ChangeSelectorItemsVisualState(true);

			if (that.IsGrouping
				|| !updatedIndex.HasValue
				|| !(dragEventArgs.DataView.FindRawData(DragItemsFormatId) is List<object> movedItems))
			{
				return;
			}

			var unwrappedSource = that.UnwrapItemsSource();
			switch (unwrappedSource)
			{
				case null: // The ListView was created with items defined in XAML
					var items = that.Items;
					ProcessMove(
						items.Count,
						items.IndexOf,
						(oldIndex, newIndex) => DoMove(items, oldIndex, newIndex));
					break;

				case IObservableVector vector:
					ProcessMove(
						vector.Count,
						vector.IndexOf,
						(oldIndex, newIndex) => DoMove(vector, oldIndex, newIndex));
					break;

				case IList list when IsObservableCollection(list):
					// The UWP ListView seems to automatically push back changes only if the ItemsSource inherits from ObservableCollection
					// Note: UWP does not use the Move method on ObservableCollection!
					ProcessMove(
						list.Count,
						list.IndexOf,
						(oldIndex, newIndex) => DoMove(list, oldIndex, newIndex));
					break;

				case ICollectionView view when !view.IsReadOnly:
					ProcessMove(
						view.Count,
						view.IndexOf,
						(oldIndex, newIndex) => DoMove(view, oldIndex, newIndex));
					break;
			}

			void ProcessMove(
				int count,
				Func<object, int> indexOf,
				Action<int, int> mv)
			{
				var indexOfDraggedItem = indexOf(item);
				if (indexOfDraggedItem < 0)
				{
					return;
				}

				try
				{
					that._isProcessingReorder = true;

					int newIndex;
					if (updatedIndex.Value.Row == int.MaxValue)
					{
						// I.e. we are at the end, there is no items below
						newIndex = count - 1;
					}
					else
					{
						newIndex = that.GetIndexFromIndexPath(updatedIndex.Value);
#if !__IOS__ // This correction doesn't apply on iOS
						if (indexOfDraggedItem < newIndex)
						{
							// If we've moved items down, we have to take in consideration that the updatedIndex
							// is already assuming that the item has been removed, so it's offsetted by 1.
							newIndex--;
						}
#endif
					}

					// When moving more than one item (multi-select), we keep their actual order in the list, no matter which one was dragged.
					movedItems.Sort((it1, it2) => indexOf(it1).CompareTo(indexOf(it2)));

					for (var i = 0; i < movedItems.Count; i++)
					{
						var movedItem = movedItems[i];
						var oldIndex = indexOf(movedItem);

						if (oldIndex < 0 || oldIndex == newIndex)
						{
							continue; // Item removed or already at the right place, nothing to do.
						}

						var restoreSelection = that.SelectedIndex == oldIndex;

						mv(oldIndex, newIndex);

						if (restoreSelection)
						{
							that.SelectedIndex = newIndex;
						}

						if (oldIndex > newIndex)
						{
							newIndex++;
						}
					}
				}
				finally
				{
					that._isProcessingReorder = false;
				}
			}
		}

		/// <summary>
		/// Update reordering information
		/// </summary>
		/// <param name="location">The current location of the pointer use to reordering items, in the ListView coordinates space</param>
		/// <param name="draggedContainer">The container that has been clicked to initiate the reordering (i.e. drag) operation (cf. remarks)</param>
		/// <param name="draggedItem">The item that has been clicked to initiate the reordering (i.e. drag) operation (cf. remarks)</param>
		/// <remarks>
		/// If the SelectionMode is not None or Single, the draggedItem/Container might not be the single that is being reordered.
		/// However, UWP hides in the ListView only the item that is being clicked by the user to initiate the reorder / drag operation.
		/// </remarks>
		private void UpdateReordering(Point location, FrameworkElement draggedContainer, object draggedItem)
			=> VirtualizingPanel?.GetLayouter().UpdateReorderingItem(location, draggedContainer, draggedItem);

		private Uno.UI.IndexPath? CompleteReordering(FrameworkElement draggedContainer, object draggedItem)
			=> VirtualizingPanel?.GetLayouter().CompleteReorderingItem(draggedContainer, draggedItem);

		private void CleanupReordering()
			=> VirtualizingPanel?.GetLayouter().CleanupReordering();

		#region Helpers
		private static bool IsObservableCollection(object src)
			=> src.GetType() is { IsGenericType: true } srcType
				&& srcType.GetGenericTypeDefinition() == typeof(ObservableCollection<>);

		private static void DoMove(ItemCollection items, int oldIndex, int newIndex)
		{
			var item = items[oldIndex];
			items.RemoveAt(oldIndex);
			if (newIndex >= items.Count)
			{
				items.Add(item);
			}
			else
			{
				items.Insert(newIndex, item);
			}
		}

		private static void DoMove(IObservableVector vector, int oldIndex, int newIndex)
		{
			var item = vector[oldIndex];
			vector.RemoveAt(oldIndex);
			if (newIndex >= vector.Count)
			{
				vector.Add(item);
			}
			else
			{
				vector.Insert(newIndex, item);
			}
		}

		private static void DoMove(IList list, int oldIndex, int newIndex)
		{
			var item = list[oldIndex];
			list.RemoveAt(oldIndex);
			if (newIndex >= list.Count)
			{
				list.Add(item);
			}
			else
			{
				list.Insert(newIndex, item);
			}
		}

		private static void DoMove(ICollectionView list, int oldIndex, int newIndex)
		{
			var item = list[oldIndex];
			list.RemoveAt(oldIndex);
			if (newIndex >= list.Count)
			{
				list.Add(item);
			}
			else
			{
				list.Insert(newIndex, item);
			}
		}
		#endregion
	}
}
