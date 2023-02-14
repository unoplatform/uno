#nullable enable

using Uno.UI.Xaml.Core;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal class UnoFocusInputHandler
	{
		private readonly RootVisual _rootVisual;

		private bool _isShiftDown;

		public UnoFocusInputHandler(RootVisual rootVisual)
		{
			_rootVisual = rootVisual;
			_rootVisual.KeyDown += OnKeyDown;
			_rootVisual.KeyUp += OnKeyUp;
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

			if (e.OriginalKey == VirtualKey.Up ||
				e.OriginalKey == VirtualKey.Down ||
				e.OriginalKey == VirtualKey.Left ||
				e.OriginalKey == VirtualKey.Right)
			{
				e.Handled = TryHandleDirectionalFocus(e.OriginalKey);
			}
		}

		internal bool TryHandleTabFocus(bool isShiftDown)
		{
			var direction = isShiftDown ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
			var contentRoot = VisualTree.GetContentRootForElement(_rootVisual);
			if (contentRoot == null)
			{
				return false;
			}

			contentRoot.InputManager.LastInputDeviceType = InputDeviceType.Keyboard;

			var focusManager = VisualTree.GetFocusManagerForElement(_rootVisual);
			var focusMovement = new FocusMovement(XYFocusOptions.Default, direction, null);
			focusMovement.IsShiftPressed = _isShiftDown;
			focusMovement.IsProcessingTab = true;
			var result = focusManager?.FindAndSetNextFocus(focusMovement);
			return result?.WasMoved == true;
		}

		internal bool TryHandleDirectionalFocus(VirtualKey originalKey)
		{
			var contentRoot = VisualTree.GetContentRootForElement(_rootVisual);
			if (contentRoot == null)
			{
				return false;
			}
			contentRoot.InputManager.LastInputDeviceType = InputDeviceType.Keyboard;

			var focusManager = VisualTree.GetFocusManagerForElement(_rootVisual);
			var focusDirection = FocusSelection.GetNavigationDirectionForKeyboardArrow(originalKey);

			if (focusManager == null || focusDirection == FocusNavigationDirection.None)
			{
				return false;
			}

			var source = focusManager.FocusedElement; // Uno specific: This should actually bubble up with the event from the source element to the root visual.

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
}
