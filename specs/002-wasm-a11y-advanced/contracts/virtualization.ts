/**
 * Virtualized Semantic Region API Contract
 * Manages semantic DOM elements for virtualized list/grid containers.
 */

/**
 * Creates a semantic element for a newly-realized item in a virtualized container.
 * Called from C# when ItemsRepeater.ElementPrepared or ListViewBase equivalent fires.
 *
 * @param containerHandle - Handle of the virtualizing container
 * @param itemHandle - Handle of the realized item's Visual
 * @param index - Data source index of the item
 * @param totalCount - Total items in the data source (for aria-setsize)
 * @param x - X position relative to container
 * @param y - Y position relative to container
 * @param width - Item width
 * @param height - Item height
 * @param role - ARIA role for the item (e.g., "option", "row")
 * @param label - Accessible label text
 */
declare function addVirtualizedItem(
    containerHandle: number,
    itemHandle: number,
    index: number,
    totalCount: number,
    x: number,
    y: number,
    width: number,
    height: number,
    role: string,
    label: string
): void;

/**
 * Removes a semantic element for an unrealized item.
 * Called from C# when ItemsRepeater.ElementClearing fires.
 * Batched via requestAnimationFrame to prevent layout thrashing.
 *
 * @param itemHandle - Handle of the item being unrealized
 */
declare function removeVirtualizedItem(itemHandle: number): void;

/**
 * Updates the total item count for a virtualized container.
 * Updates aria-setsize on all realized items.
 *
 * @param containerHandle - Handle of the virtualizing container
 * @param totalCount - New total item count
 */
declare function updateVirtualizedItemCount(
    containerHandle: number,
    totalCount: number
): void;

/**
 * Notifies that a virtualized container has been registered for accessibility tracking.
 * Creates the container's listbox/grid semantic element.
 *
 * @param containerHandle - Handle of the virtualizing container
 * @param role - "listbox" or "grid"
 * @param label - Accessible label for the container
 * @param multiselectable - Whether multiple items can be selected
 */
declare function registerVirtualizedContainer(
    containerHandle: number,
    role: string,
    label: string,
    multiselectable: boolean
): void;

/**
 * Unregisters a virtualized container and removes all its semantic elements.
 *
 * @param containerHandle - Handle of the virtualizing container
 */
declare function unregisterVirtualizedContainer(containerHandle: number): void;
