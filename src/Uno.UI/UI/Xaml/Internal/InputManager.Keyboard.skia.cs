using System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	internal KeyboardManager Keyboard { get; private set; } = default!;

	partial void ConstructKeyboardManager() => Keyboard = new(this);

	partial void InitializeKeyboard(object host) => Keyboard.Init(host);

	internal class KeyboardManager
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
				throw new InvalidOperationException("Failed to initialize the PointerManager: cannot resolve the IUnoCorePointerInputSource.");
			}

			if (_inputManager.ContentRoot.Type == ContentRootType.CoreWindow)
			{
				CoreWindow.GetForCurrentThread()?.SetKeyboardInputSource(_source);
			}

			_source.KeyDown += (s, e) => InitiateKeyDownBubblingFlow(e);
			_source.KeyUp += (s, e) => InitiateKeyUpBubblingFlow(e);
		}

		private void InitiateKeyDownBubblingFlow(KeyEventArgs args)
		{
			var originalSource = FocusManager.GetFocusedElement() as UIElement ?? Window.Current.Content;

			originalSource.RaiseEvent(
				UIElement.KeyDownEvent,
				new KeyRoutedEventArgs(originalSource, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus)
				{
					CanBubbleNatively = false
				}
			);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace(
					$"CoreWindow_KeyDown(vk: {args.VirtualKey}, " +
					$"IsExtendedKey: {args.KeyStatus.IsExtendedKey}, " +
					$"IsKeyReleased: {args.KeyStatus.IsKeyReleased}, " +
					$"IsMenuKeyDown: {args.KeyStatus.IsMenuKeyDown}, " +
					$"RepeatCount: {args.KeyStatus.RepeatCount}, " +
					$"ScanCode: {args.KeyStatus.ScanCode})"
				);
			}
		}

		private void InitiateKeyUpBubblingFlow(KeyEventArgs args)
		{
			var originalSource = FocusManager.GetFocusedElement() as UIElement ?? Window.Current.Content;

			originalSource.RaiseEvent(
				UIElement.KeyUpEvent,
				new KeyRoutedEventArgs(originalSource, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus)
				{
					CanBubbleNatively = false
				}
			);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace(
					$"CoreWindow_KeyUp(vk: {args.VirtualKey}, " +
					$"IsExtendedKey: {args.KeyStatus.IsExtendedKey}, " +
					$"IsKeyReleased: {args.KeyStatus.IsKeyReleased}, " +
					$"IsMenuKeyDown: {args.KeyStatus.IsMenuKeyDown}, " +
					$"RepeatCount: {args.KeyStatus.RepeatCount}, " +
					$"ScanCode: {args.KeyStatus.ScanCode})"
				);
			}
		}
	}
}
