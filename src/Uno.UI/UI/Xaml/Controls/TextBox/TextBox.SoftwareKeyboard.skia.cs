#nullable enable
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox
{
	// A single OS keyboard exists, so this is shared across all TextBoxes: a focus hand-off
	// (TextBox -> TextBox) bumps it and cancels the previous box's pending hide (no flicker).
	private static int _softwareKeyboardShowGeneration;

	private void ShowSoftwareKeyboardForTouchFocus(FocusState focusState)
	{
		var lastInputDeviceType = VisualTree.GetContentRootForElement(this)?.InputManager.LastInputDeviceType;
		global::System.Console.WriteLine($"[SoftKeyboard] ShowForTouchFocus reached: focusState={focusState}, lastInput={lastInputDeviceType}"); // DIAG

		// Only pointer-driven focus, and only when the pointer was touch or pen — mouse,
		// keyboard and programmatic focus must not raise the keyboard.
		if (focusState != FocusState.Pointer)
		{
			global::System.Console.WriteLine("[SoftKeyboard] gate: skipped (focusState != Pointer)"); // DIAG
			return;
		}

		if (lastInputDeviceType is not (InputDeviceType.Touch or InputDeviceType.Pen))
		{
			global::System.Console.WriteLine("[SoftKeyboard] gate: skipped (lastInput not Touch/Pen)"); // DIAG
			return;
		}

		_softwareKeyboardShowGeneration++;

		var inputPane = InputPane.GetForCurrentView();
		inputPane.TargetXamlRoot = XamlRoot;
		var shown = inputPane.TryShow();
		global::System.Console.WriteLine($"[SoftKeyboard] InputPane.TryShow() returned {shown}; Visible={inputPane.Visible}"); // DIAG
	}

	private void RequestHideSoftwareKeyboard()
	{
		var generation = _softwareKeyboardShowGeneration;
		var dispatcher = DispatcherQueue;

		if (dispatcher is null)
		{
			InputPane.GetForCurrentView().TryHide();
			return;
		}

		// Defer one tick: if another editable control raises the keyboard meanwhile it bumps
		// the generation, and we leave it shown.
		dispatcher.TryEnqueue(() =>
		{
			if (_softwareKeyboardShowGeneration == generation)
			{
				InputPane.GetForCurrentView().TryHide();
			}
		});
	}
}
