#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Tracks the accessibility state of a virtualized container
/// (ItemsRepeater, ListView, GridView). Creates/removes semantic DOM elements
/// as items are realized/unrealized, maintaining correct aria-posinset/aria-setsize.
/// </summary>
internal sealed partial class VirtualizedSemanticRegion : IDisposable
{
	private readonly IntPtr _containerHandle;
	private readonly Dictionary<int, IntPtr> _realizedHandles = new();
	// Reverse ownership keeps membership O(1) and distinguishes a moved container
	// from a stale clearing notification for its previous index.
	private readonly Dictionary<IntPtr, int> _realizedIndices = new();
	private int _totalItemCount;
	private bool _isFocusPinned;
	private int? _pinnedIndex;
	private bool _disposed;

	/// <summary>
	/// Initializes a new virtualized semantic region and registers it in the DOM.
	/// </summary>
	/// <param name="containerHandle">Handle of the container visual.</param>
	/// <param name="role">ARIA role ("listbox" or "grid").</param>
	/// <param name="label">Accessible name for the container.</param>
	/// <param name="multiselectable">Whether multiple items can be selected.</param>
	internal VirtualizedSemanticRegion(IntPtr containerHandle, string role, string? label, bool multiselectable)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Register container={containerHandle} role='{role}' label='{label}' multiselectable={multiselectable}");
		}
		_containerHandle = containerHandle;
		NativeMethods.RegisterVirtualizedContainer(containerHandle, role, label ?? string.Empty, multiselectable);
	}

	/// <summary>Gets the handle of the virtualized container visual.</summary>
	internal IntPtr ContainerHandle => _containerHandle;
	/// <summary>Gets the total number of items in the data source.</summary>
	internal int TotalItemCount => _totalItemCount;
	/// <summary>Gets whether a focused item is pinned to prevent recycling.</summary>
	internal bool IsFocusPinned => _isFocusPinned;
	/// <summary>Gets the data index of the pinned (focused) item, if any.</summary>
	internal int? PinnedIndex => _pinnedIndex;
	/// <summary>True if the given item handle currently has a realized DOM node in this region.</summary>
	internal bool ContainsRealizedHandle(IntPtr handle) => _realizedIndices.ContainsKey(handle);

	/// <summary>
	/// Called when an item is realized (ElementPrepared).
	/// </summary>
	internal void OnItemRealized(IntPtr itemHandle, int index, int totalCount, float x, float y, float width, float height, string role, string label)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ItemRealized container={_containerHandle} item={itemHandle} index={index} total={totalCount} role='{role}' label='{label}' pos=({x},{y}) size={width}x{height}");
		}
		_totalItemCount = totalCount;
		if (_realizedHandles.TryGetValue(index, out var existing) && existing != itemHandle)
		{
			_realizedIndices.Remove(existing);
		}
		SetRealizedIndex(itemHandle, index);
		NativeMethods.AddVirtualizedItem(_containerHandle, itemHandle, index, totalCount, x, y, width, height, role, label);
		WebAssemblyAccessibility.Instance.QueueRelationshipRefresh();
	}

	internal void OnItemIndexChanged(IntPtr itemHandle, int index, int totalCount)
	{
		// Decorative items receive index notifications too, but have no semantic ownership.
		if (!_realizedIndices.ContainsKey(itemHandle))
		{
			return;
		}

		_totalItemCount = totalCount;
		// A shifted index can still be occupied until that container's own notification arrives.
		// Unlike replacement during preparation, shifting must not evict the other live handle.
		SetRealizedIndex(itemHandle, index);
		NativeMethods.UpdateVirtualizedItemIndex(itemHandle, index, totalCount);
	}

	private void SetRealizedIndex(IntPtr itemHandle, int index)
	{
		if (_realizedIndices.TryGetValue(itemHandle, out var previousIndex)
			&& _realizedHandles.TryGetValue(previousIndex, out var previousHandle)
			&& previousHandle == itemHandle)
		{
			_realizedHandles.Remove(previousIndex);
		}
		_realizedHandles[index] = itemHandle;
		_realizedIndices[itemHandle] = index;
	}

	/// <summary>
	/// Called when an item is unrealized (ElementClearing).
	/// </summary>
	internal void OnItemUnrealized(IntPtr itemHandle, int index)
	{
		var hasCurrentIndex = _realizedIndices.TryGetValue(itemHandle, out var currentIndex);
		if (hasCurrentIndex && index >= 0 && currentIndex != index)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ItemUnrealized skipped (stale index) container={_containerHandle} item={itemHandle} index={index} currentIndex={currentIndex}");
			}
			return;
		}

		// Don't remove if focus-pinned
		if (_isFocusPinned && _pinnedIndex == index)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ItemUnrealized skipped (focus-pinned) container={_containerHandle} item={itemHandle} index={index}");
			}
			return;
		}

		// A removed explicit container may no longer have an index in its ItemsControl.
		if (index < 0 && hasCurrentIndex)
		{
			index = currentIndex;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ItemUnrealized container={_containerHandle} item={itemHandle} index={index}");
		}

		// Only clear the index mapping when it still points at the same handle. If a new item was
		// realized into this index before the unrealize callback arrived (race that OnItemRealized
		// already partially handles), the index now belongs to a different live handle and must
		// not be evicted. The handle being unrealized is purged from the reverse mapping
		// independently so DOM/state stay in sync.
		if (_realizedHandles.TryGetValue(index, out var current) && current == itemHandle)
		{
			_realizedHandles.Remove(index);
		}
		_realizedIndices.Remove(itemHandle);
		NativeMethods.RemoveVirtualizedItem(itemHandle);
		WebAssemblyAccessibility.Instance.QueueRelationshipRefresh();
	}

	/// <summary>
	/// Updates the total item count (e.g., when data source changes).
	/// </summary>
	internal void UpdateItemCount(int totalCount)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"UpdateItemCount container={_containerHandle} oldCount={_totalItemCount} newCount={totalCount}");
		}
		_totalItemCount = totalCount;
		NativeMethods.UpdateVirtualizedItemCount(_containerHandle, totalCount);
	}

	/// <summary>
	/// Pins a focused item to prevent it from being recycled.
	/// </summary>
	internal void PinFocusedItem(int index)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"PinFocusedItem container={_containerHandle} index={index}");
		}
		_isFocusPinned = true;
		_pinnedIndex = index;
	}

	/// <summary>
	/// Unpins the focused item.
	/// </summary>
	internal void UnpinFocusedItem()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"UnpinFocusedItem container={_containerHandle} wasIndex={_pinnedIndex}");
		}
		_isFocusPinned = false;
		_pinnedIndex = null;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Dispose container={_containerHandle} realizedCount={_realizedHandles.Count}");
			}
			_disposed = true;
			_realizedHandles.Clear();
			_realizedIndices.Clear();
			NativeMethods.UnregisterVirtualizedContainer(_containerHandle);
			WebAssemblyAccessibility.Instance.QueueRelationshipRefresh();
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.registerVirtualizedContainer")]
		internal static partial void RegisterVirtualizedContainer(IntPtr containerHandle, string role, string label, bool multiselectable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.addVirtualizedItem")]
		internal static partial void AddVirtualizedItem(IntPtr containerHandle, IntPtr itemHandle, int index, int totalCount, float x, float y, float width, float height, string role, string label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.removeVirtualizedItem")]
		internal static partial void RemoveVirtualizedItem(IntPtr itemHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateVirtualizedItemIndex")]
		internal static partial void UpdateVirtualizedItemIndex(IntPtr itemHandle, int index, int totalCount);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateVirtualizedItemCount")]
		internal static partial void UpdateVirtualizedItemCount(IntPtr containerHandle, int totalCount);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.unregisterVirtualizedContainer")]
		internal static partial void UnregisterVirtualizedContainer(IntPtr containerHandle);
	}
}
