#nullable enable

using DirectUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Windows.System;

namespace Uno.UI.Xaml.Input;

internal class UnoFocusInputHandler
{
	private readonly UIElement _rootElement;

	public UnoFocusInputHandler(UIElement rootElement)
	{
		_rootElement = rootElement;
		_rootElement.KeyDown += OnKeyDown;

#if __WASM__
		//Uno WASM specific - set tabindex to 0 so the RootVisual is "native focusable"
		rootElement.SetAttribute("tabindex", "0");
#endif
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Handled)
		{
			return;
		}

		bool isShiftDown = e.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);

		if (e.OriginalKey == VirtualKey.Tab)
		{
			e.Handled = TryHandleTabFocus(isShiftDown);
		}

		if (e.OriginalKey is
			VirtualKey.Up or
			VirtualKey.GamepadDPadUp or
			VirtualKey.Down or
			VirtualKey.GamepadDPadDown or
			VirtualKey.Left or
			VirtualKey.GamepadDPadLeft or
			VirtualKey.Right or
			VirtualKey.GamepadDPadRight)
		{
			e.Handled = TryHandleDirectionalFocus(e.OriginalKey);
		}

		if (!e.Handled && !e.HandledShouldNotImpedeTextInput)
		{
			var modifiers = CoreImports.Input_GetKeyboardModifiers();
			if (!KeyboardAcceleratorUtility.IsKeyValidForAccelerators(e.Key, KeyboardAcceleratorUtility.MapVirtualKeyModifiersToIntegersModifiers(modifiers)))
			{
				return;
			}

			var contentRoot = VisualTree.GetContentRootForElement(_rootElement);
			if (contentRoot == null)
			{
				return;
			}

			var liveAccelerators = contentRoot.GetAllLiveKeyboardAccelerators();
			e.Handled = KeyboardAcceleratorUtility.ProcessGlobalAccelerators(
				e.OriginalKey,
				modifiers,
				liveAccelerators
			);
		}
	}

	internal bool TryHandleTabFocus(bool isShiftDown)
	{
		var direction = isShiftDown ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
		var contentRoot = VisualTree.GetContentRootForElement(_rootElement);
		if (contentRoot == null)
		{
			return false;
		}

		contentRoot.InputManager.LastInputDeviceType = InputDeviceType.Keyboard;

		bool focusDeparted = false;
		void OnFocusDeparting(object s, object e)
		{
			focusDeparted = true;
		}

		var focusManager = VisualTree.GetFocusManagerForElement(_rootElement);
		if (focusManager is null)
		{
			return false;
		}

		try
		{
			var focusMovement = new FocusMovement(XYFocusOptions.Default, direction, null);
			focusMovement.IsShiftPressed = isShiftDown;
			focusMovement.IsProcessingTab = true;
			focusMovement.ForceBringIntoView = true;

			focusManager.FocusObserver.FocusController.FocusDeparting += OnFocusDeparting;

			var result = focusManager?.FindAndSetNextFocus(focusMovement);
			return result?.WasMoved == true && !focusDeparted;
		}
		finally
		{
			focusManager.FocusObserver.FocusController.FocusDeparting -= OnFocusDeparting;
		}
	}

	internal bool TryHandleDirectionalFocus(VirtualKey originalKey)
	{
		var contentRoot = VisualTree.GetContentRootForElement(_rootElement);
		if (contentRoot == null)
		{
			return false;
		}
		contentRoot.InputManager.LastInputDeviceType = InputDeviceType.Keyboard;

		var focusManager = VisualTree.GetFocusManagerForElement(_rootElement);
		// Uno specific: We are handling Gamepad input along with keyboard here.
		var focusDirection = FocusSelection.GetNavigationDirection(originalKey);

		if (focusManager == null || focusDirection == FocusNavigationDirection.None)
		{
			return false;
		}

		// Uno specific: This should actually bubble up with the event from the source element to the root visual.
		var source = focusManager.FocusedElement;

		var directionalFocusEnabled = false;
		var focusCandidateFound = false;
		bool handled = false;
		while (source != null && !focusCandidateFound)
		{
			var directionalFocusInfo = FocusSelection.TryDirectionalFocus(focusManager, focusDirection, source);
			handled |= directionalFocusInfo.Handled;

			focusCandidateFound |= directionalFocusInfo.FocusCandidateFound;
			directionalFocusEnabled |= directionalFocusInfo.DirectionalFocusEnabled;

			if (!directionalFocusInfo.ShouldBubble)
			{
				break;
			}

			if (!focusCandidateFound)
			{
				source = source.GetParent() as DependencyObject;
			}
		}

		// Only raise NoFocusCandidateFound if XYDirectionalFocus was ever set to enabled and a
		// focus candidate was never found.
		if (directionalFocusEnabled && !focusCandidateFound)
		{
			focusManager.RaiseNoFocusCandidateFoundEvent(focusDirection);
		}

		return handled;
	}
}
