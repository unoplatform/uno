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
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(
						"Failed to initialize the PointerManager: cannot resolve the IUnoKeyboardInputSource.");
				}
				return;
			}

			if (_inputManager.ContentRoot.Type == ContentRootType.CoreWindow)
			{
				CoreWindow.GetForCurrentThreadSafe()?.SetKeyboardInputSource(_source);
			}

			_source.KeyDown += (_, e) => OnKey(e, true);
			_source.KeyUp += (_, e) => OnKey(e, false);
		}

		private void OnKey(KeyEventArgs args, bool down)
		{
			var originalSource = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			var args1 = new KeyRoutedEventArgs(originalSource, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
			{
				CanBubbleNatively = false
			};
			var args2 = new KeyRoutedEventArgs(originalSource, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
			{
				CanBubbleNatively = false
			};

			originalSource.RaiseTunnelingEvent(down ? UIElement.PreviewKeyDownEvent : UIElement.PreviewKeyUpEvent, args1);
			args2.Handled = args1.Handled; // WinUI doesn't reuse the same args object, but copies the Handled value
			originalSource.RaiseEvent(down ? UIElement.KeyDownEvent : UIElement.KeyUpEvent, args2);

			args.Handled = args2.Handled;

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
	}
}
