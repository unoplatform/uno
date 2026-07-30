#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyAccessibility
{
	// An open ComboBox dropdown hosts its options under a Popup whose only semantic
	// ancestor is a role="dialog" node. Per WAI-ARIA, role="option" is only valid inside
	// a role="listbox", so without a listbox the browser invalidates the orphaned options
	// (they resolve to "paragraph") and screen readers cannot navigate them.
	//
	// ComboBox is a Selector (not an ItemsRepeater/ListViewBase), so it never goes through
	// TryRegisterVirtualizedContainer. We give each open dropdown its own role="listbox"
	// region (reusing the virtualized-container DOM plumbing), parent the options under it,
	// and link the combobox "head" to the listbox via aria-controls/aria-activedescendant.
	private readonly Dictionary<ComboBox, VirtualizedSemanticRegion> _comboBoxListBoxes = new();
	private readonly HashSet<ComboBox> _trackedComboBoxes = new();
	// Reverse Popup -> ComboBox lookup so IsComboBoxDropdownPopup is O(1) on a hot path
	// (semantic tree walk consults it for every Popup it encounters). Populated lazily on
	// DropDownOpened (the Popup may not exist at registration time) and cleared on close.
	private readonly Dictionary<Popup, ComboBox> _comboBoxByPopup = new();

	/// <summary>
	/// Subscribes to a ComboBox's dropdown lifecycle so its listbox region can be torn down
	/// when the dropdown closes. Safe to call repeatedly for the same ComboBox.
	/// </summary>
	private void TryRegisterComboBox(UIElement element)
	{
		if (element is ComboBox comboBox && _trackedComboBoxes.Add(comboBox))
		{
			comboBox.DropDownOpened += OnComboBoxDropDownOpened;
			comboBox.DropDownClosed += OnComboBoxDropDownClosed;
		}
	}

	/// <summary>
	/// Unsubscribes from a ComboBox and disposes any open listbox region. Called when the
	/// ComboBox itself leaves the visual tree.
	/// </summary>
	private void TryUnregisterComboBox(UIElement element)
	{
		if (element is ComboBox comboBox && _trackedComboBoxes.Remove(comboBox))
		{
			comboBox.DropDownOpened -= OnComboBoxDropDownOpened;
			comboBox.DropDownClosed -= OnComboBoxDropDownClosed;
			if (comboBox.GetPopup() is { } popup)
			{
				_comboBoxByPopup.Remove(popup);
			}
			DisposeComboBoxListBox(comboBox);
		}
	}

	/// <summary>
	/// True when <paramref name="popup"/> is the dropdown Popup of a tracked ComboBox. Resolved
	/// via the reverse <see cref="_comboBoxByPopup"/> lookup so this stays O(1) on the semantic
	/// tree walk; falls back to a linear scan only if the dropdown hasn't opened yet (in which
	/// case there is at most a small number of tracked ComboBoxes to check).
	/// </summary>
	private bool IsComboBoxDropdownPopup(Popup popup)
	{
		if (_comboBoxByPopup.ContainsKey(popup))
		{
			return true;
		}

		// Fallback: a Popup that exists before DropDownOpened has fired (or before this
		// instance ever opened) won't be in the reverse map yet. Keep the linear scan so
		// suppression still works at first paint; subsequent calls hit the O(1) path.
		foreach (var comboBox in _trackedComboBoxes)
		{
			if (comboBox.GetPopup() == popup)
			{
				_comboBoxByPopup[popup] = comboBox;
				return true;
			}
		}

		return false;
	}

	private void OnComboBoxDropDownOpened(object? sender, object e)
	{
		if (sender is ComboBox comboBox && comboBox.GetPopup() is { } popup)
		{
			_comboBoxByPopup[popup] = comboBox;
		}
	}

	private void OnComboBoxDropDownClosed(object? sender, object e)
	{
		if (sender is ComboBox comboBox)
		{
			if (comboBox.GetPopup() is { } popup)
			{
				_comboBoxByPopup.Remove(popup);
			}
			DisposeComboBoxListBox(comboBox);
		}
	}

	private void DisposeComboBoxListBox(ComboBox comboBox)
	{
		if (_comboBoxListBoxes.TryGetValue(comboBox, out var region))
		{
			region.Dispose();
			_comboBoxListBoxes.Remove(comboBox);

			// Drop the head's relationships to the now-removed listbox.
			NativeMethods.UpdateAriaControls(comboBox.Visual.Handle, string.Empty);
			NativeMethods.UpdateActiveDescendant(comboBox.Visual.Handle, IntPtr.Zero);
		}
	}

	/// <summary>
	/// Emits a realized dropdown item as a role="option" under the ComboBox's listbox region.
	/// No-op for anything that isn't a ComboBoxItem of an open dropdown.
	/// </summary>
	private void TryRealizeComboBoxItem(UIElement element)
	{
		if (element is not ComboBoxItem item)
		{
			return;
		}

		if (ItemsControl.ItemsControlFromItemContainer(item) is not ComboBox comboBox)
		{
			return;
		}

		// While the dropdown is closed the selected container is hosted in the faceplate
		// ContentPresenter; the head already announces the value, so don't build a listbox.
		if (!comboBox.IsDropDownOpen)
		{
			return;
		}

		var index = comboBox.IndexFromContainer(item);
		if (index < 0)
		{
			return;
		}

		var region = GetOrCreateComboBoxListBox(comboBox, item);
		if (region is null)
		{
			return;
		}

		var totalCount = comboBox.Items.Count;
		var offset = GetOffsetRelativeToSemanticParent(item, region.ContainerHandle);
		var label = item.GetOrCreateAutomationPeer()?.GetName() ?? string.Empty;

		region.OnItemRealized(
			item.Visual.Handle,
			index,
			totalCount,
			offset.X, offset.Y,
			item.Visual.Size.X, item.Visual.Size.Y,
			"option",
			label);

		// Point aria-activedescendant at the selected option so the combobox head
		// announces the active item without moving DOM focus off the head.
		if (item.IsSelected)
		{
			NativeMethods.UpdateActiveDescendant(comboBox.Visual.Handle, item.Visual.Handle);
		}
	}

	/// <summary>
	/// Removes a dropdown item's option from its ComboBox listbox region (e.g. on recycle or
	/// when the dropdown closes). No-op for anything that isn't a tracked ComboBoxItem.
	/// </summary>
	private void TryUnrealizeComboBoxItem(UIElement element)
	{
		if (element is ComboBoxItem item &&
			ItemsControl.ItemsControlFromItemContainer(item) is ComboBox comboBox &&
			_comboBoxListBoxes.TryGetValue(comboBox, out var region))
		{
			region.OnItemUnrealized(item.Visual.Handle, comboBox.IndexFromContainer(item));
		}
	}

	private VirtualizedSemanticRegion? GetOrCreateComboBoxListBox(ComboBox comboBox, ComboBoxItem item)
	{
		if (_comboBoxListBoxes.TryGetValue(comboBox, out var existing))
		{
			return existing;
		}

		// Key the listbox node by the items host (CarouselPanel) handle so it is distinct
		// from the combobox "head" element, which carries role="combobox" under the same
		// uno-semantics-{handle} id scheme.
		var itemsHost = comboBox.ItemsPanelRoot ?? item.GetParent() as Panel;
		if (itemsHost is null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] ComboBox dropdown has no items host; cannot create listbox region for handle={comboBox.Visual.Handle}");
			}
			return null;
		}

		var label = comboBox.GetOrCreateAutomationPeer()?.GetName();
		var region = new VirtualizedSemanticRegion(
			itemsHost.Visual.Handle,
			"listbox",
			label,
			multiselectable: false);
		_comboBoxListBoxes[comboBox] = region;

		// WAI-ARIA combobox pattern: the head owns the popup listbox via aria-controls so
		// screen readers associate the two separate DOM subtrees and aria-activedescendant
		// can reference options that live outside the head's own subtree.
		NativeMethods.UpdateAriaControls(comboBox.Visual.Handle, $"uno-semantics-{region.ContainerHandle}");

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] Created ComboBox listbox region: head={comboBox.Visual.Handle} listbox={region.ContainerHandle} label='{label}'");
		}

		return region;
	}
}
