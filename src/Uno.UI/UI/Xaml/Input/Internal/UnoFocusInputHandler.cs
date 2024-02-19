#nullable enable

using Uno.UI.Xaml.Core;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input;

internal class UnoFocusInputHandler
{
	private readonly UIElement _rootElement;

	private bool _isShiftDown;

	public UnoFocusInputHandler(UIElement rootElement)
	{
		_rootElement = rootElement;
		_rootElement.KeyDown += OnKeyDown;
		_rootElement.KeyUp += OnKeyUp;
	}

	private void OnKeyUp(object sender, KeyRoutedEventArgs e)
	{
		if (e.OriginalKey == VirtualKey.Shift ||
			e.OriginalKey == VirtualKey.LeftShift ||
			e.OriginalKey == VirtualKey.RightShift)
		{
			_isShiftDown = false;
		}
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.OriginalKey == VirtualKey.Shift ||
			e.OriginalKey == VirtualKey.LeftShift ||
			e.OriginalKey == VirtualKey.RightShift)
		{
			_isShiftDown = true;
		}

		if (e.Handled)
		{
			return;
		}

		if (e.OriginalKey == VirtualKey.Tab)
		{
			e.Handled = TryHandleTabFocus(_isShiftDown);
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

		var focusManager = VisualTree.GetFocusManagerForElement(_rootElement);
		var focusMovement = new FocusMovement(XYFocusOptions.Default, direction, null);
		focusMovement.IsShiftPressed = _isShiftDown;
		focusMovement.IsProcessingTab = true;
		focusMovement.ForceBringIntoView = true;
		var result = focusManager?.FindAndSetNextFocus(focusMovement);
		return result?.WasMoved == true;
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
