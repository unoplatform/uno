#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Represents a focus trap boundary for a modal dialog (ContentDialog).
/// Manages aria-hidden on background elements, Tab/Shift+Tab wrapping,
/// nested modal support via ParentScope linked list, and focus restoration on close.
/// </summary>
internal sealed partial class ModalFocusScope
{
	private readonly IntPtr _modalHandle;
	private readonly IntPtr _triggerHandle;
	private readonly List<IntPtr> _focusableChildren;
	private ModalFocusScope? _parentScope;
	private bool _isActive;

	/// <summary>
	/// Initializes a new modal focus scope for a dialog.
	/// </summary>
	/// <param name="modalHandle">Handle of the modal dialog visual.</param>
	/// <param name="triggerHandle">Handle of the element that had focus before the dialog opened.</param>
	/// <param name="focusableChildren">Handles of focusable elements within the dialog.</param>
	internal ModalFocusScope(IntPtr modalHandle, IntPtr triggerHandle, List<IntPtr> focusableChildren)
	{
		_modalHandle = modalHandle;
		_triggerHandle = triggerHandle;
		_focusableChildren = focusableChildren;
	}

	/// <summary>Gets the handle of the modal dialog visual.</summary>
	internal IntPtr ModalHandle => _modalHandle;
	/// <summary>Gets the handle of the element that triggered the dialog.</summary>
	internal IntPtr TriggerHandle => _triggerHandle;
	/// <summary>Gets the parent scope for nested modal support.</summary>
	internal ModalFocusScope? ParentScope => _parentScope;
	/// <summary>Gets whether this focus trap is currently active.</summary>
	internal bool IsActive => _isActive;

	/// <summary>
	/// Activates this focus trap. Sets aria-hidden on background and starts Tab wrapping.
	/// </summary>
	internal void Activate(ModalFocusScope? parentScope)
	{
		Console.WriteLine($"[A11y] MODAL FOCUS: Activate modal={_modalHandle} trigger={_triggerHandle} focusableChildren={_focusableChildren.Count} hasParent={parentScope is not null}");
		_parentScope = parentScope;
		_isActive = true;

		var handles = new int[_focusableChildren.Count];
		for (int i = 0; i < _focusableChildren.Count; i++)
		{
			handles[i] = _focusableChildren[i].ToInt32();
		}

		NativeMethods.ActivateFocusTrap(_modalHandle, _triggerHandle, handles);
	}

	/// <summary>
	/// Deactivates this focus trap. Restores aria-hidden and focus to trigger element.
	/// </summary>
	internal void Deactivate()
	{
		Console.WriteLine($"[A11y] MODAL FOCUS: Deactivate modal={_modalHandle} trigger={_triggerHandle}");
		_isActive = false;
		NativeMethods.DeactivateFocusTrap(_modalHandle);
	}

	/// <summary>
	/// Updates focusable children (e.g., when buttons are enabled/disabled within the modal).
	/// </summary>
	internal void UpdateChildren(List<IntPtr> focusableChildren)
	{
		Console.WriteLine($"[A11y] MODAL FOCUS: UpdateChildren modal={_modalHandle} count={focusableChildren.Count}");
		_focusableChildren.Clear();
		_focusableChildren.AddRange(focusableChildren);

		var handles = new int[_focusableChildren.Count];
		for (int i = 0; i < _focusableChildren.Count; i++)
		{
			handles[i] = _focusableChildren[i].ToInt32();
		}

		NativeMethods.UpdateFocusTrapChildren(_modalHandle, handles);
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.FocusTrap.activateFocusTrap")]
		internal static partial void ActivateFocusTrap(IntPtr modalHandle, IntPtr triggerHandle, [JSMarshalAs<JSType.Array<JSType.Number>>] int[] focusableHandles);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.FocusTrap.deactivateFocusTrap")]
		internal static partial void DeactivateFocusTrap(IntPtr modalHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.FocusTrap.updateFocusTrapChildren")]
		internal static partial void UpdateFocusTrapChildren(IntPtr modalHandle, [JSMarshalAs<JSType.Array<JSType.Number>>] int[] focusableHandles);
	}
}
