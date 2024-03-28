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
			var originalSource1 = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			var args1 = new KeyRoutedEventArgs(originalSource1, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
			{
				CanBubbleNatively = false
			};

			originalSource1.RaiseTunnelingEvent(down ? UIElement.PreviewKeyDownEvent : UIElement.PreviewKeyUpEvent, args1);

			// On WinUI, if the focus changes during PreviewKey<Down|Up>, the Key<Up|Down> event bubbles from the new focused element.
			var originalSource2 = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			var args2 = new KeyRoutedEventArgs(originalSource2, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
			{
				CanBubbleNatively = false,
				Handled = args1.Handled // WinUI doesn't reuse the same args object, but copies the Handled value
			};

			originalSource2.RaiseEvent(down ? UIElement.KeyDownEvent : UIElement.KeyUpEvent, args2);

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

		/// <summary>
		/// ONLY USE THIS FOR TESTS
		/// </summary>
		internal void OnKeyTestingOnly(KeyEventArgs args, bool down) => OnKey(args, down);
	}
}
