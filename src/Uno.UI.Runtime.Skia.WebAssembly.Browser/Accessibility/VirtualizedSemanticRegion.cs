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

	/// <summary>
	/// Called when an item is realized (ElementPrepared).
	/// </summary>
	internal void OnItemRealized(IntPtr itemHandle, int index, int totalCount, float x, float y, float width, float height, string role, string label)
	{
		_totalItemCount = totalCount;
		_realizedHandles[index] = itemHandle;
		NativeMethods.AddVirtualizedItem(_containerHandle, itemHandle, index, totalCount, x, y, width, height, role, label);
	}

	/// <summary>
	/// Called when an item is unrealized (ElementClearing).
	/// </summary>
	internal void OnItemUnrealized(IntPtr itemHandle, int index)
	{
		// Don't remove if focus-pinned
		if (_isFocusPinned && _pinnedIndex == index)
		{
			return;
		}

		_realizedHandles.Remove(index);
		NativeMethods.RemoveVirtualizedItem(itemHandle);
	}

	/// <summary>
	/// Updates the total item count (e.g., when data source changes).
	/// </summary>
	internal void UpdateItemCount(int totalCount)
	{
		_totalItemCount = totalCount;
		NativeMethods.UpdateVirtualizedItemCount(_containerHandle, totalCount);
	}

	/// <summary>
	/// Pins a focused item to prevent it from being recycled.
	/// </summary>
	internal void PinFocusedItem(int index)
	{
		_isFocusPinned = true;
		_pinnedIndex = index;
	}

	/// <summary>
	/// Unpins the focused item.
	/// </summary>
	internal void UnpinFocusedItem()
	{
		_isFocusPinned = false;
		_pinnedIndex = null;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_realizedHandles.Clear();
			NativeMethods.UnregisterVirtualizedContainer(_containerHandle);
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

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateVirtualizedItemCount")]
		internal static partial void UpdateVirtualizedItemCount(IntPtr containerHandle, int totalCount);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.unregisterVirtualizedContainer")]
		internal static partial void UnregisterVirtualizedContainer(IntPtr containerHandle);
	}
}
