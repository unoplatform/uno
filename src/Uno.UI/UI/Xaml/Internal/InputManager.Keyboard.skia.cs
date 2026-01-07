using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using DirectUI;
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

			// Process context menu keyboard triggers (Shift+F10, Application key, GamepadMenu)
			// This matches WinUI behavior where context menu is triggered after KeyDown.
			if (down && !routedArgs.Handled)
			{
				_inputManager.ContentRoot.ContextMenuProcessor.ProcessContextRequestOnKeyboardInput(
					originalSource2,
					args.VirtualKey,
					args.KeyboardModifiers);
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
		/// ONLY USE THIS FOR TESTS
		/// </summary>
		internal void OnKeyTestingOnly(KeyEventArgs args, bool down) => OnKey(args, down);
	}
}
