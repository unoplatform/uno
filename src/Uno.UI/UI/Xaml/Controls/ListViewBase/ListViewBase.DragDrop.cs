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
using Uno.Logging;
using Uno.UI;
using _DragEventArgs = global::Windows.UI.Xaml.DragEventArgs;

namespace Windows.UI.Xaml.Controls
{
	partial class ListViewBase
	{
		private const string ReorderOwnerFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__source__";
		private const string ReorderItemFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__source__item__";
		private const string ReorderContainerFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__source__container__";
		private const string DragItemsFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__items__";

		public event DragItemsStartingEventHandler DragItemsStarting;
		public event TypedEventHandler<ListViewBase, DragItemsCompletedEventArgs> DragItemsCompleted;

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
			itemContainer.DropCompleted -= OnItemContainerDragCompleted;

			itemContainer.CanDrag = true;
			itemContainer.DragStarting += OnItemContainerDragStarting;
			itemContainer.DropCompleted += OnItemContainerDragCompleted;
		}

		private static void ClearContainerForDragDrop(UIElement itemContainer)
		{
			if (itemContainer is null) // Even if flagged as impossible by nullable check, https://github.com/unoplatform/uno/issues/4725
			{
				return;
			}

			itemContainer.DragStarting -= OnItemContainerDragStarting;
			itemContainer.DropCompleted -= OnItemContainerDragCompleted;

			itemContainer.DragEnter -= OnReorderUpdated;
			itemContainer.DragOver -= OnReorderUpdated;
			itemContainer.DragLeave -= OnReorderCompleted;
			itemContainer.Drop -= OnReorderCompleted;
		}

		private static void OnItemContainerDragStarting(UIElement sender, DragStartingEventArgs innerArgs)
		{
			if (ItemsControlFromItemContainer(sender) is ListViewBase that && that.CanDragItems)
			{
				var items = that.SelectionMode == ListViewSelectionMode.Multiple || that.SelectionMode == ListViewSelectionMode.Extended
					? that.SelectedItems.ToList()
					: new List<object>();
				var draggedItem = that.ItemFromContainer(sender);
				if (draggedItem is { } && !items.Contains(draggedItem))
				{
					items.Add(draggedItem);
				}

				var args = new DragItemsStartingEventArgs(innerArgs, items);

				that.DragItemsStarting?.Invoke(that, args);

				// The application has the ability to add some items in the list, so make sure to freeze it only after event has been raised.
				args.Data.SetData(DragItemsFormatId, args.Items.ToList());

				// The ListView must have both CanReorderItems and AllowDrop flags set to allow re-ordering (UWP)
				// We also do not allow re-ordering if we where not able to find the item (as it has to be hidden in the view) (Uno only)
				if (that.CanReorderItems && that.AllowDrop && draggedItem is {})
				{
					args.Data.SetData(ReorderOwnerFormatId, that);
					args.Data.SetData(ReorderItemFormatId, draggedItem);
					args.Data.SetData(ReorderContainerFormatId, sender);

					// For safety only, avoids double subscription
					that.DragEnter -= OnReorderUpdated;
					that.DragOver -= OnReorderUpdated;
					that.DragLeave -= OnReorderCompleted;
					that.Drop -= OnReorderCompleted;

					that.DragEnter += OnReorderUpdated;
					that.DragOver += OnReorderUpdated;
					that.DragLeave += OnReorderCompleted;
					that.Drop += OnReorderCompleted;
				}
			}
		}

		private static void OnItemContainerDragCompleted(UIElement sender, DropCompletedEventArgs innerArgs)
		{
			// Note: It's not the responsibility of the ListView to remove item from the source, not matter the AcceptedOperation.

			if (ItemsControlFromItemContainer(sender) is ListViewBase that)
			{
				that.DragEnter -= OnReorderUpdated;
				that.DragOver -= OnReorderUpdated;
				that.DragLeave -= OnReorderCompleted;
				that.Drop -= OnReorderCompleted;

				if (that.CanDragItems)
				{
					var items = innerArgs.Info.Data.FindRawData(DragItemsFormatId) as IReadOnlyList<object> ?? new List<object>(0);
					var args = new DragItemsCompletedEventArgs(innerArgs, items);

					that.DragItemsCompleted?.Invoke(that, args);
				}
			}
		}

		private static void OnReorderUpdated(object sender, _DragEventArgs dragEventArgs)
		{
			var that = sender as ListView;
			var src = dragEventArgs.DataView.FindRawData(ReorderOwnerFormatId) as ListView;
			var item = dragEventArgs.DataView.FindRawData(ReorderItemFormatId);
			var container = dragEventArgs.DataView.FindRawData(ReorderContainerFormatId) as FrameworkElement; // TODO: This might have changed/been recycled if scrolled 
			if (that is null || src is null || item is null || container is null || src != that)
			{
				dragEventArgs.Log().Warn("Invalid reorder event.");

				return;
			}

			that.UpdateReordering(dragEventArgs.GetPosition(that), container, item);
		}

		private static void OnReorderCompleted(object sender, _DragEventArgs dragEventArgs)
		{
			var that = sender as ListView;
			var src = dragEventArgs.DataView.FindRawData(ReorderOwnerFormatId) as ListView;
			var item = dragEventArgs.DataView.FindRawData(ReorderItemFormatId);
			var container = dragEventArgs.DataView.FindRawData(ReorderContainerFormatId) as FrameworkElement; // TODO: This might have changed/been recycled if scrolled 
			if (that is null || src is null || item is null || container is null || src != that)
			{
				dragEventArgs.Log().Warn("Invalid reorder event.");

				return;
			}

			var updatedIndex = default(Uno.UI.IndexPath?);
			that.CompleteReordering(container, item, ref updatedIndex);

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

				int newIndex;
				if (updatedIndex.Value.Row == int.MaxValue)
				{
					// I.e. we are at the end, there is no items below
					newIndex = count - 1;
				}
				else
				{
					newIndex = that.GetIndexFromIndexPath(updatedIndex.Value);
					if (indexOfDraggedItem < newIndex)
					{
						// If we've moved items down, we have to take in consideration that the updatedIndex
						// is already assuming that the item has been removed, so it's offsetted by 1.
						newIndex--;
					}
				}

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
						// This is a workaround for https://github.com/unoplatform/uno/issues/4741
						container.SetValue(IndexForItemContainerProperty, newIndex);

						that.SelectedIndex = newIndex;
					}

					if (oldIndex > newIndex)
					{
						newIndex++;
					}
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
		partial void UpdateReordering(Point location, FrameworkElement draggedContainer, object draggedItem);

		partial void CompleteReordering(FrameworkElement draggedContainer, object draggedItem, ref Uno.UI.IndexPath? updatedIndex);

		#region Helpers
		private static bool IsObservableCollection(object src)
		{
			var srcType = src.GetType();
			return srcType.IsGenericType && srcType.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
		}

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
		#endregion
	}
}
