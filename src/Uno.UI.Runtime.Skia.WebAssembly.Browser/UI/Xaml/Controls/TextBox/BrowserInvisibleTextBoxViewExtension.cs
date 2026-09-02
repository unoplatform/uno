using System.Runtime.InteropServices.JavaScript;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.System;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserInvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private readonly TextBoxView _view;
	private bool _isNativeInputActive;
	private bool _suppressSoftwareKeyboard;

	public BrowserInvisibleTextBoxViewExtension(TextBoxView view)
	{
		_view = view;
		NativeMethods.Initialize();
	}

	private bool IsHostFocused => _view.Host is Control { FocusState: not FocusState.Unfocused };

	private int SelectionStart => _view.Host switch
	{
		TextBox textBox => textBox.SelectionStart,
		RichEditBox richEditBox => richEditBox.NativeSelectionStart,
		_ => 0,
	};

	private int SelectionLength => _view.Host switch
	{
		TextBox textBox => textBox.SelectionLength,
		RichEditBox richEditBox => richEditBox.NativeSelectionLength,
		_ => 0,
	};

	private string SelectionDirection => _view.Host switch
	{
		TextBox { IsBackwardSelection: true } => "backward",
		RichEditBox { NativeSelectionIsBackward: true } => "backward",
		_ => "forward",
	};

	[JSExport]
	private static void OnInputTextChanged(string text, int selectionStart, int selectionLength)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the TextBox is focused, as it's the result of an interaction with the native HTML input.
		switch (FocusManager.GetFocusedElement(xamlRoot!))
		{
			case TextBox textBox:
				textBox.TextBoxView.UpdateTextFromNative(text);
				textBox.SelectInternal(selectionStart, selectionLength);
				break;
			case RichEditBox richEditBox:
				richEditBox.UpdateTextFromNative(text, selectionStart, selectionLength);
				break;
		}
	}

	[JSExport]
	private static void OnNativePaste(string clipboardText)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the TextBox is focused, as it's the result of an interaction with the native HTML input.
		switch (FocusManager.GetFocusedElement(xamlRoot!))
		{
			case TextBox textBox:
				textBox.PasteFromClipboard(clipboardText);
				break;
			case RichEditBox richEditBox:
				richEditBox.PasteFromClipboard(clipboardText);
				break;
		}
	}

	[JSExport]
	private static int GetNativePasteSourceLimit()
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		return FocusManager.GetFocusedElement(xamlRoot!) switch
		{
			RichEditBox richEditBox => richEditBox.GetClipboardPasteSourceLimit(),
			TextBox { MaxLength: > 0 } textBox => GetTextBoxPasteSourceLimit(textBox),
			_ => int.MaxValue,
		};

		static int GetTextBoxPasteSourceLimit(TextBox textBox)
		{
			var selectedLength = Math.Clamp(textBox.SelectionLength, 0, textBox.Text.Length);
			var outputLimit = Math.Max(0, textBox.MaxLength - (textBox.Text.Length - selectedLength));
			return outputLimit >= (int.MaxValue - 2) / 2
				? int.MaxValue
				: outputLimit * 2 + 2;
		}
	}

	[JSExport]
	private static void OnSelectionChanged(int selectionStart, int selectionLength)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the TextBox is focused, as it's the result of an interaction with the native HTML input.
		switch (FocusManager.GetFocusedElement(xamlRoot!))
		{
			case TextBox textBox:
				textBox.SelectInternal(selectionStart, selectionLength);
				break;
			case RichEditBox richEditBox:
				richEditBox.SelectFromNative(selectionStart, selectionLength);
				break;
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

		if (FocusManager.GetFocusedElement(xamlRoot!) is Control control and (TextBox or RichEditBox))
		{
			var keyArgs = new KeyRoutedEventArgs(control, VirtualKey.Enter, VirtualKeyModifiers.None);
			control.RaiseEvent(UIElement.KeyDownEvent, keyArgs);
		}
	}

	// The "overlay layer" is the DOM, which is always present.
	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry(bool suppressSoftwareKeyboard = false)
	{
		_suppressSoftwareKeyboard = suppressSoftwareKeyboard;
		var host = _view.Host;
		_isNativeInputActive = NativeMethods.Focus(
			(host as UIElement)?.Visual.Handle ?? 0,
			_view.IsPasswordBox,
			host?.Text,
			host switch
			{
				TextBox textBox => textBox.AcceptsReturn,
				RichEditBox richEditBox => richEditBox.AcceptsReturn,
				_ => false,
			},
			suppressSoftwareKeyboard ? "none" : GetInputModeValue(),
			GetEnterKeyHintValue());

		if (_isNativeInputActive)
		{
			InvalidateLayout(); // we create the native <input /> object in Focus, so we should make sure to update the layout
			NativeMethods.SetTextPredictionEnabled(GetTextPredictionEnabled());
			NativeMethods.SetSpellCheckEnabled(GetSpellCheckEnabled());
			NativeMethods.UpdateSelection(SelectionStart, SelectionLength, SelectionDirection);
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
				NativeMethods.Blur(_view.TextBox?.Visual.Handle ?? 0);
			}
			_isNativeInputActive = false;
			_suppressSoftwareKeyboard = false;
		}
	}

	internal static void DetachNativeInputPreservingFocus() => NativeMethods.Detach();

	internal static void InvalidateComposition() => NativeMethods.InvalidateComposition();

	internal static void RestartComposition() => NativeMethods.RestartComposition();

	public void UpdateSize()
	{
		if (!IsHostFocused)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		NativeMethods.UpdateSize(_view.DisplayBlock.ActualWidth, _view.DisplayBlock.ActualHeight);
	}

	public void UpdatePosition()
	{
		if (!IsHostFocused)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		var p = _view.DisplayBlock.TransformToVisual(null).TransformPoint(default);
		NativeMethods.UpdatePosition(p.X, p.Y);
	}

	public void NotifyImePositionChanged()
	{
		if (IsHostFocused)
		{
			NativeMethods.UpdateSelection(SelectionStart, SelectionLength, SelectionDirection);
		}
	}

	public void InvalidateLayout()
	{
		UpdateSize();
		UpdatePosition();
	}

	public void SetText(string text)
	{
		if (!IsHostFocused)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		NativeMethods.SetText(text);
	}

	public void ReplaceText(int start, int length, string replacement)
	{
		if (!IsHostFocused)
		{
			return;
		}
		NativeMethods.ReplaceText(start, length, replacement);
	}

	public void Select(int start, int length)
	{
		if (!IsHostFocused)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		NativeMethods.UpdateSelection(start, length, SelectionDirection);
	}

	// Since we don't actually use the <input /> visually, do we don't need to take care of any of the visual aspects
	public void UpdateNativeView() { }
	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }
	public void UpdateProperties()
	{
		if (!IsHostFocused)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		if (GetEnterKeyHintValue() is { } enterKeyHintValue)
		{
			NativeMethods.SetEnterKeyHint(enterKeyHintValue);
		}
		NativeMethods.SetInputMode(_suppressSoftwareKeyboard ? "none" : GetInputModeValue());
		NativeMethods.SetTextPredictionEnabled(GetTextPredictionEnabled());
		NativeMethods.SetSpellCheckEnabled(GetSpellCheckEnabled());
		NativeMethods.SetAcceptsReturn(GetAcceptsReturn());
	}

	public int GetSelectionStart() => 0;
	public int GetSelectionLength() => 0;
	public int GetSelectionStartBeforeKeyDown() => 0;
	public int GetSelectionLengthBeforeKeyDown() => 0;

	private string GetEnterKeyHintValue()
	{
		if (_view.Host is TextBox textBox)
		{
			return TextBoxExtensions.GetInputReturnType(textBox).ToEnterKeyHintValue();
		}

		return "";
	}

	private string GetInputModeValue()
	{
		if (_view.Host is TextBox textBox)
		{
			return textBox.InputScope.ToInputModeValue();
		}
		if (_view.Host is RichEditBox richEditBox)
		{
			return richEditBox.InputScope.ToInputModeValue();
		}
		return "";
	}

	private bool GetTextPredictionEnabled() => _view.Host switch
	{
		TextBox textBox => textBox.IsTextPredictionEnabled,
		RichEditBox richEditBox => richEditBox.IsTextPredictionEnabled,
		_ => true,
	};

	private bool GetSpellCheckEnabled() => _view.Host?.IsSpellCheckEnabled ?? true;

	private bool GetAcceptsReturn() => _view.Host is IImeSessionHost { AcceptsReturn: true };

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.initialize")]
		public static partial void Initialize();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setText")]
		public static partial void SetText(string text);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.replaceText")]
		public static partial void ReplaceText(int start, int length, string replacement);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.invalidateComposition")]
		public static partial void InvalidateComposition();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.restartComposition")]
		public static partial void RestartComposition();

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

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setTextPredictionEnabled")]
		public static partial void SetTextPredictionEnabled(bool enabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setSpellCheckEnabled")]
		public static partial void SetSpellCheckEnabled(bool enabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setAcceptsReturn")]
		public static partial void SetAcceptsReturn(bool acceptsReturn);
	}
}
