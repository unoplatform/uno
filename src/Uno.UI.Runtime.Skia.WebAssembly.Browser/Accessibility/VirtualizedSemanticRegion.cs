#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
	private readonly bool _usesActiveDescendant;
	private readonly Dictionary<int, IntPtr> _realizedHandles = new();
	// Parallel set kept in sync with _realizedHandles.Values so ContainsRealizedHandle
	// is O(1) on the focus/lookup hot path instead of O(n) Dictionary.ContainsValue.
	private readonly HashSet<IntPtr> _realizedHandleSet = new();
	private readonly HashSet<IntPtr> _pendingUnrealizedHandles = new();
	private int _totalItemCount;
	private IntPtr _pinnedHandle;
	private bool _disposed;

	/// <summary>
	/// Initializes a new virtualized semantic region and registers it in the DOM.
	/// </summary>
	/// <param name="containerHandle">Handle of the container visual.</param>
	/// <param name="role">ARIA role ("listbox" or "grid").</param>
	/// <param name="label">Accessible name for the container.</param>
	/// <param name="multiselectable">Whether multiple items can be selected.</param>
	internal VirtualizedSemanticRegion(IntPtr containerHandle, string role, string? label, bool multiselectable, bool usesActiveDescendant = false)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Register container={containerHandle} role='{role}' labelLength={label?.Length ?? 0} multiselectable={multiselectable}");
		}
		_containerHandle = containerHandle;
		_usesActiveDescendant = usesActiveDescendant;
		NativeMethods.RegisterVirtualizedContainer(containerHandle, role, label ?? string.Empty, multiselectable, usesActiveDescendant);
	}

	/// <summary>Gets the handle of the virtualized container visual.</summary>
	internal IntPtr ContainerHandle => _containerHandle;
	/// <summary>Gets the total number of items in the data source.</summary>
	internal int TotalItemCount => _totalItemCount;
	/// <summary>Gets whether a focused item is pinned to prevent recycling.</summary>
	internal bool IsFocusPinned => _pinnedHandle != IntPtr.Zero;
	/// <summary>Gets the handle of the pinned (focused) item, if any.</summary>
	internal IntPtr PinnedHandle => _pinnedHandle;
	/// <summary>True if the given item handle currently has a realized DOM node in this region.</summary>
	internal bool ContainsRealizedHandle(IntPtr handle) => _realizedHandleSet.Contains(handle);
	internal IntPtr[] GetRealizedHandles() => [.. _realizedHandleSet];

	internal void UpdateMultiselectable(bool multiselectable)
		=> NativeMethods.UpdateVirtualizedContainerMultiselectable(_containerHandle, multiselectable);

	/// <summary>
	/// Called when an item is realized (ElementPrepared).
	/// </summary>
	internal IntPtr OnItemRealized(
		IntPtr itemHandle,
		int index,
		int totalCount,
		float x,
		float y,
		float width,
		float height,
		string role,
		string label,
		bool selected,
		bool disabled,
		bool focusable)
	{
		if (_disposed)
		{
			return IntPtr.Zero;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ItemRealized container={_containerHandle} item={itemHandle} index={index} total={totalCount} role='{role}' labelLength={label.Length} pos=({x},{y}) size={width}x{height}");
		}
		if (!NativeMethods.AddVirtualizedItem(_containerHandle, itemHandle, index, totalCount, x, y, width, height, role, label, selected, disabled, focusable, _usesActiveDescendant))
		{
			return IntPtr.Zero;
		}

		_totalItemCount = totalCount;
		_pendingUnrealizedHandles.Remove(itemHandle);
		var removedHandle = IntPtr.Zero;
		if (_realizedHandles.TryGetValue(index, out var existing) && existing != itemHandle)
		{
			if (existing == _pinnedHandle)
			{
				_pendingUnrealizedHandles.Add(existing);
			}
			else
			{
				_realizedHandleSet.Remove(existing);
				removedHandle = existing;
			}
		}
		_realizedHandles[index] = itemHandle;
		_realizedHandleSet.Add(itemHandle);
		if (removedHandle != IntPtr.Zero)
		{
			TryRemoveVirtualizedItem(removedHandle);
		}
		return removedHandle;
	}

	/// <summary>
	/// Called when an item is unrealized (ElementClearing).
	/// </summary>
	internal bool OnItemUnrealized(IntPtr itemHandle, int index)
	{
		if (_disposed)
		{
			return false;
		}

		// Keep the focused node until focus leaves. The underlying ItemsRepeater/ListView owns
		// visual pinning; this tombstone keeps semantic membership consistent if a collection
		// change forces a clear despite that visual pin.
		if (_pinnedHandle == itemHandle)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ItemUnrealized deferred (focus-pinned) container={_containerHandle} item={itemHandle} index={index}");
			}
			if (_realizedHandles.TryGetValue(index, out var pinnedAtIndex) && pinnedAtIndex == itemHandle)
			{
				_realizedHandles.Remove(index);
			}
			_pendingUnrealizedHandles.Add(itemHandle);
			return false;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ItemUnrealized container={_containerHandle} item={itemHandle} index={index}");
		}

		// Only clear the index mapping when it still points at the same handle. If a new item was
		// realized into this index before the unrealize callback arrived (race that OnItemRealized
		// already partially handles), the index now belongs to a different live handle and must
		// not be evicted. The handle being unrealized is always purged from _realizedHandleSet
		// independently so DOM/state stay in sync.
		if (_realizedHandles.TryGetValue(index, out var current) && current == itemHandle)
		{
			_realizedHandles.Remove(index);
		}
		_realizedHandleSet.Remove(itemHandle);
		TryRemoveVirtualizedItem(itemHandle);
		return true;
	}

	/// <summary>
	/// Updates the total item count (e.g., when data source changes).
	/// </summary>
	internal void UpdateItemCount(int totalCount)
	{
		if (_disposed)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"UpdateItemCount container={_containerHandle} oldCount={_totalItemCount} newCount={totalCount}");
		}
		_totalItemCount = totalCount;
		NativeMethods.UpdateVirtualizedItemCount(_containerHandle, totalCount);
	}

	internal void OnItemIndexChanged(IntPtr itemHandle, int oldIndex, int newIndex, int totalCount)
	{
		if (_disposed)
		{
			return;
		}

		if (_realizedHandles.TryGetValue(oldIndex, out var oldHandle) && oldHandle == itemHandle)
		{
			_realizedHandles.Remove(oldIndex);
		}
		_realizedHandles[newIndex] = itemHandle;
		_totalItemCount = totalCount;
		NativeMethods.UpdateVirtualizedItemPosition(itemHandle, newIndex, totalCount);
	}

	internal IntPtr[] ResynchronizeItems(IEnumerable<(IntPtr Handle, int Index)> items, int totalCount)
	{
		if (_disposed)
		{
			return [];
		}

		var removedHandles = new List<IntPtr>();
		var synchronizedHandles = new HashSet<IntPtr>();
		var synchronizedItems = new List<(IntPtr Handle, int Index)>();
		foreach (var (handle, index) in items)
		{
			if (NativeMethods.UpdateVirtualizedItemPosition(handle, index, totalCount))
			{
				synchronizedHandles.Add(handle);
				synchronizedItems.Add((handle, index));
			}
		}
		foreach (var staleHandle in _realizedHandleSet.ToArray())
		{
			if (!synchronizedHandles.Contains(staleHandle))
			{
				if (staleHandle == _pinnedHandle)
				{
					_pendingUnrealizedHandles.Add(staleHandle);
				}
				else
				{
					_realizedHandleSet.Remove(staleHandle);
					removedHandles.Add(staleHandle);
					TryRemoveVirtualizedItem(staleHandle);
				}
			}
		}
		_realizedHandles.Clear();
		_realizedHandleSet.Clear();
		foreach (var (handle, index) in synchronizedItems)
		{
			_realizedHandles[index] = handle;
			_realizedHandleSet.Add(handle);
		}
		if (_pinnedHandle != IntPtr.Zero && _pendingUnrealizedHandles.Contains(_pinnedHandle))
		{
			_realizedHandleSet.Add(_pinnedHandle);
		}
		_totalItemCount = totalCount;
		NativeMethods.UpdateVirtualizedItemCount(_containerHandle, totalCount);
		return [.. removedHandles];
	}

	/// <summary>
	/// Pins a focused item to prevent it from being recycled.
	/// </summary>
	internal IntPtr PinFocusedItem(IntPtr handle)
	{
		if (!_realizedHandleSet.Contains(handle))
		{
			return IntPtr.Zero;
		}

		var removedHandle = _pinnedHandle != handle ? UnpinFocusedItem() : IntPtr.Zero;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"PinFocusedItem container={_containerHandle} handle={handle}");
		}
		_pinnedHandle = handle;
		return removedHandle;
	}

	/// <summary>
	/// Unpins the focused item.
	/// </summary>
	internal IntPtr UnpinFocusedItem()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"UnpinFocusedItem container={_containerHandle} wasHandle={_pinnedHandle}");
		}

		var handle = _pinnedHandle;
		_pinnedHandle = IntPtr.Zero;
		if (handle == IntPtr.Zero || !_pendingUnrealizedHandles.Remove(handle))
		{
			return IntPtr.Zero;
		}

		foreach (var item in _realizedHandles.Where(item => item.Value == handle).ToArray())
		{
			_realizedHandles.Remove(item.Key);
		}
		_realizedHandleSet.Remove(handle);
		TryRemoveVirtualizedItem(handle);
		return handle;
	}

	private void TryRemoveVirtualizedItem(IntPtr handle)
	{
		try
		{
			NativeMethods.RemoveVirtualizedItem(handle);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to remove virtualized semantic item {handle} from container {_containerHandle}: {ex.Message}", ex);
			}
		}
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
			_realizedHandleSet.Clear();
			_pendingUnrealizedHandles.Clear();
			_pinnedHandle = IntPtr.Zero;
			NativeMethods.UnregisterVirtualizedContainer(_containerHandle);
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.registerVirtualizedContainer")]
		internal static partial void RegisterVirtualizedContainer(IntPtr containerHandle, string role, string label, bool multiselectable, bool usesActiveDescendant);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.addVirtualizedItem")]
		internal static partial bool AddVirtualizedItem(IntPtr containerHandle, IntPtr itemHandle, int index, int totalCount, float x, float y, float width, float height, string role, string label, bool selected, bool disabled, bool focusable, bool usesActiveDescendant);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.removeVirtualizedItem")]
		internal static partial void RemoveVirtualizedItem(IntPtr itemHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateVirtualizedItemCount")]
		internal static partial void UpdateVirtualizedItemCount(IntPtr containerHandle, int totalCount);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateVirtualizedItemPosition")]
		internal static partial bool UpdateVirtualizedItemPosition(IntPtr itemHandle, int index, int totalCount);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateVirtualizedContainerMultiselectable")]
		internal static partial void UpdateVirtualizedContainerMultiselectable(IntPtr containerHandle, bool multiselectable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.unregisterVirtualizedContainer")]
		internal static partial void UnregisterVirtualizedContainer(IntPtr containerHandle);
	}
}
