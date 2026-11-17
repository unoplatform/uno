#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime;

/// <summary>
/// Tracks the accessibility state of a virtualized container
/// (ItemsRepeater, ListView, GridView). Creates/removes semantic DOM elements
/// as items are realized/unrealized, maintaining correct aria-posinset/aria-setsize.
/// </summary>
internal sealed partial class VirtualizedSemanticRegion : IDisposable
{
	private readonly IntPtr _containerHandle;
	private readonly Dictionary<int, IntPtr> _realizedHandles = new();
	private readonly Dictionary<IntPtr, int> _realizedIndices = new();
	// Index a recycled container was realized at before being re-realized elsewhere, so a late
	// unrealize for that old index can be told apart from one for the live realization.
	private readonly Dictionary<IntPtr, int> _supersededIndices = new();
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

	/// <summary>Unsubscribes the container events that feed this region; run on dispose.</summary>
	internal Action? Detach { get; set; }

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
	internal void OnItemRealized(IntPtr itemHandle, int index, int totalCount, float x, float y, float width, float height, string role, string label, bool? selected, string automationId)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ItemRealized container={_containerHandle} item={itemHandle} index={index} total={totalCount} role='{role}' label='{label}' pos=({x},{y}) size={width}x{height}");
		}
		_totalItemCount = totalCount;
		if (_realizedHandles.TryGetValue(index, out var existing) && existing != itemHandle
			&& _realizedIndices.TryGetValue(existing, out var existingIndex) && existingIndex == index)
		{
			_realizedIndices.Remove(existing);
		}
		if (_realizedIndices.TryGetValue(itemHandle, out var previousIndex) && previousIndex != index)
		{
			_supersededIndices[itemHandle] = previousIndex;
			if (_realizedHandles.TryGetValue(previousIndex, out var previous) && previous == itemHandle)
			{
				_realizedHandles.Remove(previousIndex);
			}
		}
		_realizedHandles[index] = itemHandle;
		_realizedIndices[itemHandle] = index;
		NativeMethods.AddVirtualizedItem(_containerHandle, itemHandle, index, totalCount, x, y, width, height, role, label, selected, automationId);
		WebAssemblyAccessibility.Instance.QueueRelationshipRefresh();
	}

	/// <summary>
	/// Called when an item is unrealized (ElementClearing).
	/// </summary>
	internal void OnItemUnrealized(IntPtr itemHandle, int index)
	{
		// Don't remove if focus-pinned
		if (_isFocusPinned && _pinnedIndex == index)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ItemUnrealized skipped (focus-pinned) container={_containerHandle} item={itemHandle} index={index}");
			}
			return;
		}

		// A late callback for the index a container was re-realized away from must not evict the live node.
		if (_supersededIndices.TryGetValue(itemHandle, out var supersededIndex) && supersededIndex == index)
		{
			_supersededIndices.Remove(itemHandle);
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ItemUnrealized skipped (stale, re-realized) container={_containerHandle} item={itemHandle} index={index}");
			}
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ItemUnrealized container={_containerHandle} item={itemHandle} index={index}");
		}

		if (_realizedHandles.TryGetValue(index, out var current) && current == itemHandle)
		{
			_realizedHandles.Remove(index);
		}
		_realizedIndices.Remove(itemHandle);
		_supersededIndices.Remove(itemHandle);
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
			Detach?.Invoke();
			Detach = null;
			_realizedHandles.Clear();
			_realizedIndices.Clear();
			_supersededIndices.Clear();
			NativeMethods.UnregisterVirtualizedContainer(_containerHandle);
			WebAssemblyAccessibility.Instance.QueueRelationshipRefresh();
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.SemanticElements.registerVirtualizedContainer")]
		internal static partial void RegisterVirtualizedContainer(IntPtr containerHandle, string role, string label, bool multiselectable);

		[JSImport("globalThis.Uno.UI.Runtime.SemanticElements.addVirtualizedItem")]
		internal static partial void AddVirtualizedItem(IntPtr containerHandle, IntPtr itemHandle, int index, int totalCount, float x, float y, float width, float height, string role, string label, bool? selected, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.SemanticElements.removeVirtualizedItem")]
		internal static partial void RemoveVirtualizedItem(IntPtr itemHandle);

		[JSImport("globalThis.Uno.UI.Runtime.SemanticElements.updateVirtualizedItemCount")]
		internal static partial void UpdateVirtualizedItemCount(IntPtr containerHandle, int totalCount);

		[JSImport("globalThis.Uno.UI.Runtime.SemanticElements.unregisterVirtualizedContainer")]
		internal static partial void UnregisterVirtualizedContainer(IntPtr containerHandle);
	}
}
