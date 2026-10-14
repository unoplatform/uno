#nullable enable
using System;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

internal interface IInputInjectorTarget
{
	void InjectPointerAdded(PointerEventArgs args);

	void InjectPointerUpdated(PointerEventArgs args);

	void InjectPointerRemoved(PointerEventArgs args);

	void InjectKeyDown(KeyEventArgs args);

	void InjectKeyUp(KeyEventArgs args);

	/// <summary>
	/// Gets a value indicating whether the window owning this target is currently activated.
	/// Keyboard injection targets the active window, mirroring the foreground-window behavior
	/// of the Windows implementation.
	/// </summary>
	bool IsActive { get; }

	/// <summary>Gets whether this target is initialized and attached to a live window.</summary>
	bool IsReadyForKeyboardInput { get; }
	/// <summary>Gets whether the live window is visible, for host fallback targeting.</summary>
	bool IsVisibleForKeyboardInput { get; }
}
