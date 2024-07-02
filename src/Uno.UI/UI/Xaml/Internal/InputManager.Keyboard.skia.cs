using System;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

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

			var routedArgs = new KeyRoutedEventArgs(originalSource1, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey)
			{
				CanBubbleNatively = false
			};

			originalSource1.RaiseTunnelingEvent(down ? UIElement.PreviewKeyDownEvent : UIElement.PreviewKeyUpEvent, routedArgs);

			// On WinUI, if the focus changes during PreviewKey<Down|Up>, the Key<Up|Down> event bubbles from the new focused element.
			var originalSource2 = FocusManager.GetFocusedElement(_inputManager.ContentRoot.XamlRoot) as UIElement ?? _inputManager.ContentRoot.VisualTree.RootElement;

			// WinUI doesn't reuse the same args object, but creates a new routed args object and copies the Handled value
			// To reduce allocations, we reuse the same routed args object twice.
			originalSource2.RaiseEvent(down ? UIElement.KeyDownEvent : UIElement.KeyUpEvent, routedArgs);

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
		}

		/// <summary>
		/// ONLY USE THIS FOR TESTS
		/// </summary>
		internal void OnKeyTestingOnly(KeyEventArgs args, bool down) => OnKey(args, down);
	}
}
