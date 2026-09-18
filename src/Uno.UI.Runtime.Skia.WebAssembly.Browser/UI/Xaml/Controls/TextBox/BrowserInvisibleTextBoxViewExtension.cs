using System.Runtime.InteropServices.JavaScript;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using Windows.System;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserInvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private readonly TextBoxView _view;
	private bool _isNativeInputActive;

	public BrowserInvisibleTextBoxViewExtension(TextBoxView view)
	{
		_view = view;
		NativeMethods.Initialize();
	}

	private string SelectionDirection => _view.Core is { IsBackwardSelection: true } ? "backward" : "forward";

	[JSExport]
	private static void OnInputTextChanged(string text, int selectionStart, int selectionLength)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the control is focused, as it's the result of an interaction with the native HTML input.
		if (FocusManager.GetFocusedElement(xamlRoot!) is ITextBoxHost { Core: { } core })
		{
			core.TextBoxView.UpdateTextFromNative(text);
			core.SelectInternal(selectionStart, selectionLength);
		}
	}

	[JSExport]
	private static void OnNativePaste(string clipboardText)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the control is focused, as it's the result of an interaction with the native HTML input.
		if (FocusManager.GetFocusedElement(xamlRoot!) is ITextBoxHost { Core: { } core })
		{
			core.PasteFromClipboard(clipboardText);
		}
	}

	[JSExport]
	private static void OnSelectionChanged(int selectionStart, int selectionLength)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the control is focused, as it's the result of an interaction with the native HTML input.
		if (FocusManager.GetFocusedElement(xamlRoot!) is ITextBoxHost { Core: { } core })
		{
			core.SelectInternal(selectionStart, selectionLength);
		}
	}

	[JSExport]
	private static void OnNativeBlur()
	{
		try
		{
			var xamlRoot = WebAssemblyWindowWrapper.Instance?.XamlRoot;
			if (xamlRoot is null)
			{
				// Fired before the window/XamlRoot exists or during teardown: no managed focus to re-sync.
				return;
			}

			// Fired only for browser-initiated blurs of the shared native input (managed-initiated
			// blurs are suppressed on the JS side). In that case managed focus is still stale on the
			// TextBox, so clearing it here is what finally raises LostFocus and re-syncs FocusManager.
			var focused = FocusManager.GetFocusedElement(xamlRoot);
			if (typeof(BrowserInvisibleTextBoxViewExtension).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(BrowserInvisibleTextBoxViewExtension).Log().Trace($"OnNativeBlur: focused element is {focused?.GetType().Name ?? "null"}");
			}
			if (focused is TextBox textBox)
			{
				textBox.Unfocus();
			}
		}
		catch (Exception e)
		{
			// A managed exception must not cross back into the JS DOM-event callback.
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	[JSExport]
	private static void OnEnterKeyPressed()
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;

		if (FocusManager.GetFocusedElement(xamlRoot!) is ITextBoxHost { Core: { } core })
		{
			var keyArgs = new KeyRoutedEventArgs(core.Owner, VirtualKey.Enter, VirtualKeyModifiers.None);
			core.Owner.RaiseEvent(UIElement.KeyDownEvent, keyArgs);
		}
	}

	// The "overlay layer" is the DOM, which is always present.
	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry()
	{
		_isNativeInputActive = NativeMethods.Focus(
			_view.Core?.Owner.Visual.Handle ?? 0,
			_view.IsPasswordBox,
			_view.Core?.Text,
			_view.Core?.AcceptsReturn ?? false,
			GetInputModeValue(),
			GetEnterKeyHintValue());

		if (_isNativeInputActive)
		{
			InvalidateLayout(); // we create the native <input /> object in Focus, so we should make sure to update the layout
			NativeMethods.UpdateSelection(_view.Core?.SelectionStart ?? 0, _view.Core?.SelectionLength ?? 0, SelectionDirection);
		}
	}

	public void EndEntry()
	{
		if (_isNativeInputActive)
		{
			if (NativeMethods.HasInput())
			{
				// The handle lets the JS side ignore this blur when another TextBox has already
				// taken over the shared input (focus moving between TextBoxes).
				NativeMethods.Blur(_view.Core?.Owner.Visual.Handle ?? 0);
			}
			_isNativeInputActive = false;
		}
	}

	internal static void DetachNativeInputPreservingFocus() => NativeMethods.Detach();

	public void UpdateSize()
	{
		if (!_view.Core?.Owner.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all text controls, so only propagate state from
			// managed to native when this control is the one in focus
			return;
		}

		if (_view.Core?.ContentElement is not { } contentElement)
		{
			return;
		}

		// We deliberately track the ContentElement and not the DisplayBlock: the DisplayBlock shrink-wraps the
		// text, so using it would make the <input /> grow by one character's width on every keystroke. Browser
		// password managers anchor their affordances to the <input /> bounds, so a growing element makes them
		// visibly drift across the field. This matches OverlayTextBoxViewExtension on the other Skia targets.
		// Advisory: on iOS the JS side keeps the input off-screen and ignores size and position.
		NativeMethods.UpdateSize(
			Math.Max(0, contentElement.ActualWidth - contentElement.Padding.Horizontal()),
			Math.Max(0, contentElement.ActualHeight - contentElement.Padding.Vertical()));
	}

	public void UpdatePosition()
	{
		if (!_view.Core?.Owner.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all text controls, so only propagate state from
			// managed to native when this control is the one in focus
			return;
		}

		if (_view.Core?.ContentElement is not { } contentElement)
		{
			return;
		}

		// Anchored on the ContentElement for the same reason as UpdateSize: the DisplayBlock moves as the text
		// scrolls or is re-aligned, which would drag the <input /> away from the field it belongs to.
		// Advisory: on iOS the JS side keeps the input off-screen and ignores size and position.
		var p = contentElement
			.TransformToVisual(null)
			.TransformPoint(new Point(contentElement.Padding.Left, contentElement.Padding.Top));
		NativeMethods.UpdatePosition(p.X, p.Y);
	}

	public void InvalidateLayout()
	{
		UpdateSize();
		UpdatePosition();
	}

	public void SetText(string text)
	{
		if (!_view.Core?.Owner.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all text controls, so only propagate state from
			// managed to native when this control is the one in focus
			return;
		}
		NativeMethods.SetText(text);
	}

	public void Select(int start, int length)
	{
		if (!_view.Core?.Owner.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all text controls, so only propagate state from
			// managed to native when this control is the one in focus
			return;
		}
		NativeMethods.UpdateSelection(start, length, SelectionDirection);
	}

	// Since we don't actually use the <input /> visually, do we don't need to take care of any of the visual aspects
	public void UpdateNativeView() { }
	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }
	public void UpdateProperties()
	{
		if (!_view.Core?.Owner.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all text controls, so only propagate state from
			// managed to native when this control is the one in focus
			return;
		}
		if (GetEnterKeyHintValue() is { } enterKeyHintValue)
		{
			NativeMethods.SetEnterKeyHint(enterKeyHintValue);
		}
	}

	public int GetSelectionStart() => 0;
	public int GetSelectionLength() => 0;
	public int GetSelectionStartBeforeKeyDown() => 0;
	public int GetSelectionLengthBeforeKeyDown() => 0;

	private string GetEnterKeyHintValue()
	{
		if (_view?.Core is { } core)
		{
			return TextBoxExtensions.GetInputReturnType(core.Owner).ToEnterKeyHintValue();
		}

		return "";
	}

	private string GetInputModeValue()
	{
		if (_view?.Core is { } core)
		{
			return core.InputScope.ToInputModeValue();
		}
		return "";
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.initialize")]
		public static partial void Initialize();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setText")]
		public static partial void SetText(string text);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.focus")]
		public static partial bool Focus(IntPtr handle, bool isPassword, string? text, bool acceptsReturn, string inputMode, string enterKeyHint);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.blur")]
		public static partial void Blur(IntPtr handle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.detach")]
		public static partial void Detach();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.hasInput")]
		public static partial bool HasInput();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updateSize")]
		public static partial void UpdateSize(double width, double height);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updatePosition")]
		public static partial void UpdatePosition(double x, double y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updateSelection")]
		public static partial void UpdateSelection(int start, int length, string direction);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setEnterKeyHint")]
		public static partial void SetEnterKeyHint(string setEnterKeyHint);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setInputMode")]
		public static partial void SetInputMode(string inputMode);
	}
}
