using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using DirectUI;
using Microsoft.UI.Xaml.Internal;
using Uno.UI.Core;
using Uno.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	internal KeyboardManager Keyboard { get; private set; }

	[MemberNotNull(nameof(Keyboard))]
	partial void ConstructKeyboardManager() => Keyboard = new(this);

	partial void InitializeKeyboard(object host) => Keyboard.Init(host);

	internal sealed class KeyboardManager
	{
		private readonly InputManager _inputManager;
		private IUnoKeyboardInputSource _source;

		public KeyboardManager(InputManager inputManager)
		{
			_inputManager = inputManager;
		}

		public void Init(object host)
		{
			if (!ApiExtensibility.CreateInstance(host, out _source))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(
						"Failed to initialize the PointerManager: cannot resolve the IUnoKeyboardInputSource.");
				}
				return;
			}

			CoreWindow.GetForCurrentThreadSafe()?.SetKeyboardInputSource(_source);

			_source.KeyDown += (_, e) => OnKey(e, true);
			_source.KeyUp += (_, e) => OnKey(e, false);
			_source.CharacterReceived += (_, e) => OnCharacterReceived(e);
		}

		private void OnKey(KeyEventArgs args, bool down)
		{
			SyncModifierStates(args.KeyboardModifiers);

			if (XboxUtility.IsGamepadNavigationInput(args.VirtualKey))
			{
				_inputManager.LastInputDeviceType = InputDeviceType.GamepadOrRemote;
			}
			else
			{
				_inputManager.LastInputDeviceType = InputDeviceType.Keyboard;
			}

			var originalSource1 = GetKeyRoutedSource(FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot));

			var routedArgs = new KeyRoutedEventArgs(originalSource1, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
			{
				CanBubbleNatively = false,
				Handled = args.Handled
			};

			originalSource1.RaiseTunnelingEvent(down ? UIElement.PreviewKeyDownEvent : UIElement.PreviewKeyUpEvent, routedArgs);

			// On WinUI, if the focus changes during PreviewKey<Down|Up>, the Key<Up|Down> event bubbles from the new focused element.
			var focusedElement = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot);
			var originalSource2 = GetKeyRoutedSource(focusedElement);

			// A focused text element is not a UIElement, so Uno hands it the key here; WinUI's CHyperlink
			// registers its own KeyDown/KeyUp listeners on itself instead (Hyperlink.cpp).
			if (!routedArgs.Handled && focusedElement is TextElement focusedTextElement)
			{
				if (down)
				{
					focusedTextElement.OnKeyDown(args.VirtualKey);
				}
				else
				{
					focusedTextElement.OnKeyUp(args.VirtualKey);
				}
			}

			// WinUI doesn't reuse the same args object, but creates a new routed args object and copies the Handled value
			// To reduce allocations, we reuse the same routed args object twice.
			originalSource2.RaiseEvent(down ? UIElement.KeyDownEvent : UIElement.KeyUpEvent, routedArgs);

			// Ported from: KeyboardInputProcessor.cpp OnKeyDown (lines 171-192)
			// Dismiss transient flyouts on unhandled keypress with no modifiers.
			if (down && !routedArgs.Handled
				&& args.KeyboardModifiers == VirtualKeyModifiers.None
				&& TextControlFlyoutHelper.IsElementChildOfTransientOpenedFlyout(originalSource2))
			{
				TextControlFlyoutHelper.DismissAllFlyoutsForOwner(originalSource2);
				routedArgs.Handled = true;
			}

			// Process context menu keyboard triggers (Shift+F10, Application key, GamepadMenu)
			// This matches WinUI behavior where context menu is triggered after KeyDown.
			if (down && !routedArgs.Handled)
			{
				_inputManager._contextMenuProcessor.ProcessContextRequestOnKeyboardInput(
					originalSource2,
					args.VirtualKey,
					args.KeyboardModifiers);
			}

			// On Windows a character produced by a key press is delivered as a separate message
			// (WM_CHAR follows WM_KEYDOWN), so CharacterReceived is raised after KeyDown completed,
			// targeting whichever element is focused by then. Note: raised regardless of the KeyDown
			// Handled state, as WM_CHAR generation is independent of app-side key handling.
			if (down && args.UnicodeKey is { } character)
			{
				RaiseCharacterReceived(character, args.KeyStatus);
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				var methodName = down ? "CoreWindow_KeyDown" : "CoreWindow_KeyUp";
				this.Log().Trace(
					$"{methodName}(vk: {args.VirtualKey}, " +
					$"IsExtendedKey: {args.KeyStatus.IsExtendedKey}, " +
					$"IsKeyReleased: {args.KeyStatus.IsKeyReleased}, " +
					$"IsMenuKeyDown: {args.KeyStatus.IsMenuKeyDown}, " +
					$"RepeatCount: {args.KeyStatus.RepeatCount}, " +
					$"ScanCode: {args.KeyStatus.ScanCode})"
				);
			}

			args.Handled = routedArgs.Handled;
		}

		/// <summary>
		/// Repairs the tracked modifier state from the modifiers the event carries, which report the
		/// real state of every modifier at the time it was raised. Key downs and key ups on their own
		/// cannot be trusted to stay paired - a modifier released while the browser or the OS holds
		/// focus never produces a key up - so the tracker would otherwise report it as held for the
		/// rest of the session.
		/// </summary>
		/// <remarks>
		/// Runs before the event is dispatched, so a handler reading CoreWindow.GetKeyState sees the
		/// corrected state. The key this event is about needs no special treatment: the key down/up
		/// applied while the event bubbles lands after this and wins.
		/// </remarks>
		private static void SyncModifierStates(VirtualKeyModifiers modifiers)
		{
			KeyboardStateTracker.SyncModifierState(VirtualKey.Control, modifiers.HasFlag(VirtualKeyModifiers.Control));
			KeyboardStateTracker.SyncModifierState(VirtualKey.Shift, modifiers.HasFlag(VirtualKeyModifiers.Shift));
			KeyboardStateTracker.SyncModifierState(VirtualKey.Menu, modifiers.HasFlag(VirtualKeyModifiers.Menu));

			if (!modifiers.HasFlag(VirtualKeyModifiers.Windows))
			{
				// There is no combined Windows key to correct, and a press cannot be attributed to a
				// side, so only the release - which rules out both - can be applied here.
				KeyboardStateTracker.SyncModifierState(VirtualKey.LeftWindows, false);
				KeyboardStateTracker.SyncModifierState(VirtualKey.RightWindows, false);
			}
		}

		/// <summary>
		/// Handles a composed character that never went through a key press,
		/// e.g. a Windows Alt+numpad code composed when Alt is released.
		/// </summary>
		private void OnCharacterReceived(CharacterReceivedEventArgs args)
			=> RaiseCharacterReceived((char)args.KeyCode, args.KeyStatus);

		private void RaiseCharacterReceived(char character, CorePhysicalKeyStatus keyStatus)
		{
			var originalSource = GetKeyRoutedSource(FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot));

			var routedArgs = new CharacterReceivedRoutedEventArgs(originalSource, character, keyStatus)
			{
				CanBubbleNatively = false,
			};

			originalSource.RaiseEvent(UIElement.CharacterReceivedEvent, routedArgs);
		}

		// A focused text element is not a UIElement; like WinUI's walk through non-public parents, its key
		// events bubble from the control whose inline tree holds it.
		private UIElement GetKeyRoutedSource(object focusedElement)
			=> focusedElement as UIElement
				?? (focusedElement as TextElement)?.GetContainingFrameworkElement()
				?? _inputManager.ContentRoot.VisualTree.RootElement;

		/// <summary>
		/// ONLY USE THIS FOR TESTS
		/// </summary>
		internal void OnKeyTestingOnly(KeyEventArgs args, bool down) => OnKey(args, down);
	}
}
