
#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Xaml.Controls;

// Uno's drag entry point. WinUI's ListViewBaseItem starts the drag through the core DragDrop service
// and calls OnDragGesture. Uno starts it through UIElement.CanDrag and the DragStarting event, then
// hands over to the ported OnDragGesture. The drag ends with the item's DropCompleted event, which
// OnDragGesture subscribes to.
partial class ListViewBase
{
	private const string ReorderOwnerFormatId = DataPackage.UnoPrivateDataPrefix + "__list__view__base__source__";

	// Set by TreeViewList, which reorders on its own.
	internal bool IsCustomReorder;

	// The args of the DragStarting being handled. WinUI's OnDragGesture creates its own.
	private DragStartingEventArgs? m_unoDragStartingArgs;

	// Whether the container is the primary dragged container of a live reorder of this list, which
	// Uno lays out under the pointer (WinUI leaves an empty gap there).
	internal bool IsLiveReorderPrimaryContainer(UIElement container, DragEventArgs args)
		=> !IsCustomReorder
			&& args.DataView.FindRawData(ReorderOwnerFormatId) == this
			&& IsContainerDragDropOwner(container);

	// Containers are draggable when CanDragItems is set, or when reordering is active.
	internal void PrepareContainerForDragDrop(UIElement itemContainer)
	{
		if (GetIsDragEnabled())
		{
			PrepareContainerForDragDropCore(itemContainer);
		}
	}

	internal override void ContainerClearedForItem(object item, Primitives.SelectorItem itemContainer)
	{
		ClearContainerForDragDrop(itemContainer);

		base.ContainerClearedForItem(item, itemContainer);
	}

	// To call when CanDragItems or CanReorderItems changes.
	internal void UpdateContainersForDragDrop()
	{
		var isDragEnabled = GetIsDragEnabled();

		foreach (var container in MaterializedContainers.OfType<UIElement>())
		{
			if (isDragEnabled)
			{
				PrepareContainerForDragDropCore(container);
			}
			else
			{
				ClearContainerForDragDrop(container);
			}
		}
	}

	private static void PrepareContainerForDragDropCore(UIElement? itemContainer)
	{
		if (itemContainer is null)
		{
			return;
		}

		// ContainerClearedForItem might not run for all containers. The handler is static, so it can't leak.
		itemContainer.DragStarting -= OnItemContainerDragStarting;

		itemContainer.CanDrag = true;
		itemContainer.DragStarting += OnItemContainerDragStarting;
	}

	internal static void ClearContainerForDragDrop(UIElement? itemContainer)
	{
		if (itemContainer is null)
		{
			return;
		}

		itemContainer.CanDrag = false;
		itemContainer.DragStarting -= OnItemContainerDragStarting;
	}

	private static void OnItemContainerDragStarting(UIElement sender, DragStartingEventArgs innerArgs)
	{
		if (sender is not ListViewBaseItem draggedItemContainer
			|| ItemsControlFromItemContainer(sender) is not ListViewBase that)
		{
			return;
		}

		// UIElement raises DragStarting only with a pointer event at hand.
		var lastPointerEvent = PointerRoutedEventArgs.LastPointerEvent;
		var pointer = lastPointerEvent.Pointer;
		var pointerPoint = lastPointerEvent.GetCurrentPoint(null!);

		that.m_unoDragStartingArgs = innerArgs;
		try
		{
			var wasCanceled = that.OnDragGesture(
				pointer,
				pointerPoint,
				draggedItemContainer,
				new object(), // No DragDropVisual: Uno draws the drag visual with DragUI.
				pointerPoint.Position,
				() =>
				{
					if (pointer.PointerDeviceType == PointerDeviceType.Touch)
					{
						draggedItemContainer.OnTouchDragStarted();
					}

					draggedItemContainer.OnDragGestureStarting(pointer);
				});

			if (wasCanceled)
			{
				innerArgs.Cancel = true;
			}
			else
			{
				if (that.CanReorderItems && that.AllowDrop)
				{
					innerArgs.Data.SetData(ReorderOwnerFormatId, that);
				}

				// WinUI's DragDrop::HidePrimaryDraggedItem, once the drag visual is captured. Uno captures it with DragUI
				// as the drag starts, and DragDrop::CleanupDragDrop's reset is in OnDragGesture's DropCompleted handler.
				draggedItemContainer.SetDragVisualCaptured(true);
			}
		}
		finally
		{
			that.m_unoDragStartingArgs = null;
		}
	}
}

internal enum DragDropMessageType
{
	DragEnter,
	DragOver,
	DragLeave,
	Drop,
}
