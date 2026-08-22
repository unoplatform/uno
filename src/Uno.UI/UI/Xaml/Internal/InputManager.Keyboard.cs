using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using DirectUI;
using Microsoft.UI.Xaml.Internal;
using Uno.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	internal KeyboardManager Keyboard { get; private set; }

	[MemberNotNull(nameof(Keyboard))]
	partial void ConstructKeyboardManager() => Keyboard = new(this);

	partial void InitializeKeyboard(object host) => Keyboard.Init(host);

	#region IInputInjectorTarget
	void IInputInjectorTarget.InjectKeyDown(KeyEventArgs args) => Keyboard.Inject(args, down: true);

	void IInputInjectorTarget.InjectKeyUp(KeyEventArgs args) => Keyboard.Inject(args, down: false);

	bool IInputInjectorTarget.IsActive
		=> ContentRoot.GetOwnerWindow()?.NativeWrapper?.ActivationState
			is not (null or CoreWindowActivationState.Deactivated);
	#endregion

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
			if (XboxUtility.IsGamepadNavigationInput(args.VirtualKey))
			{
				_inputManager.LastInputDeviceType = InputDeviceType.GamepadOrRemote;
			}
			else
			{
				_inputManager.LastInputDeviceType = InputDeviceType.Keyboard;
			}

			var originalSource1 = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			var routedArgs = new KeyRoutedEventArgs(originalSource1, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
			{
				CanBubbleNatively = false,
				Handled = args.Handled
			};

			originalSource1.RaiseTunnelingEvent(down ? UIElement.PreviewKeyDownEvent : UIElement.PreviewKeyUpEvent, routedArgs);

			// On WinUI, if the focus changes during PreviewKey<Down|Up>, the Key<Up|Down> event bubbles from the new focused element.
			var originalSource2 = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

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
		/// Handles a composed character that never went through a key press,
		/// e.g. a Windows Alt+numpad code composed when Alt is released.
		/// </summary>
		private void OnCharacterReceived(CharacterReceivedEventArgs args)
			=> RaiseCharacterReceived((char)args.KeyCode, args.KeyStatus);

		private void RaiseCharacterReceived(char character, CorePhysicalKeyStatus keyStatus)
		{
			var originalSource = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			var routedArgs = new CharacterReceivedRoutedEventArgs(originalSource, character, keyStatus)
			{
				CanBubbleNatively = false,
			};

			originalSource.RaiseEvent(UIElement.CharacterReceivedEvent, routedArgs);
		}

		/// <summary>
		/// Entry point for <see cref="InputInjector.InjectKeyboardInput"/>, joining the pipeline at
		/// the same place a host does so injected keys get focus routing, accelerators and text input.
		/// </summary>
		internal void Inject(KeyEventArgs args, bool down)
		{
			if (_inputManager.ContentRoot.XamlRoot is null)
			{
				// FocusManager.GetFocusedElement throws on a null XamlRoot. Injection is an
				// automation API, so a diagnosable no-op beats throwing out of a startup race.
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning("Ignoring injected key: the content root is not attached to a window yet.");
				}

				return;
			}

			OnKey(args, down);
		}

		/// <summary>
		/// ONLY USE THIS FOR TESTS
		/// </summary>
		internal void OnKeyTestingOnly(KeyEventArgs args, bool down) => OnKey(args, down);
	}
}
