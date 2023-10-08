using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

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
				throw new InvalidOperationException("Failed to initialize the PointerManager: cannot resolve the IUnoCorePointerInputSource.");
			}

			if (_inputManager.ContentRoot.Type == ContentRootType.CoreWindow)
			{
				CoreWindow.GetForCurrentThreadSafe()?.SetKeyboardInputSource(_source);
			}

			_source.KeyDown += (s, e) => InitiateKeyDownBubblingFlow(e);
			_source.KeyUp += (s, e) => InitiateKeyUpBubblingFlow(e);
		}

		private void InitiateKeyDownBubblingFlow(KeyEventArgs args)
		{
			var originalSource = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				return;
			}

			originalSource.RaiseEvent(
				UIElement.KeyDownEvent,
				new KeyRoutedEventArgs(originalSource, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
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
			var originalSource = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			if (originalSource is null)
			{
				return;
			}

			originalSource.RaiseEvent(
				UIElement.KeyUpEvent,
				new KeyRoutedEventArgs(originalSource, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
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
