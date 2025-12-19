using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserInvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private readonly TextBoxView _view;

	public BrowserInvisibleTextBoxViewExtension(TextBoxView view)
	{
		_view = view;
		NativeMethods.Initialize();
	}

	private string SelectionDirection => _view.TextBox is { IsBackwardSelection: true } ? "backward" : "forward";

	[JSExport]
	private static void OnInputTextChanged(string text, int selectionStart, int selectionLength)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the TextBox is focused, as it's the result of an interaction with the native HTML input.
		if (FocusManager.GetFocusedElement(xamlRoot!) is TextBox textBox)
		{
			textBox.TextBoxView.UpdateTextFromNative(text);
			textBox.SelectInternal(selectionStart, selectionLength);
		}
	}

	[JSExport]
	private static void OnNativePaste(string clipboardText)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the TextBox is focused, as it's the result of an interaction with the native HTML input.
		if (FocusManager.GetFocusedElement(xamlRoot!) is TextBox textBox)
		{
			textBox.PasteFromClipboard(clipboardText);
		}
	}

	[JSExport]
	private static void OnSelectionChanged(int selectionStart, int selectionLength)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// We are expecting this to be called only when the TextBox is focused, as it's the result of an interaction with the native HTML input.
		if (FocusManager.GetFocusedElement(xamlRoot!) is TextBox textBox)
		{
			textBox.SelectInternal(selectionStart, selectionLength);
		}
	}

	// The "overlay layer" is the DOM, which is always present.
	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry()
	{
		NativeMethods.Focus(_view.IsPasswordBox, _view.TextBox?.Text, _view.TextBox?.AcceptsReturn ?? false, GetInputModeValue(), GetEnterKeyHintValue(), _view.TextBox?.IsSpellCheckEnabled ?? false);
		InvalidateLayout(); // we create the native <input /> object in Focus, so we should make sure to update the layout
		NativeMethods.UpdateSelection(_view.TextBox?.SelectionStart ?? 0, _view.TextBox?.SelectionLength ?? 0, SelectionDirection);
	}

	public void EndEntry() => NativeMethods.Blur();

	public void UpdateSize()
	{
		if (!_view.TextBox?.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		NativeMethods.UpdateSize(_view.DisplayBlock.ActualWidth, _view.DisplayBlock.ActualHeight);
	}

	public void UpdatePosition()
	{
		if (!_view.TextBox?.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		var p = _view.DisplayBlock.TransformToVisual(null).TransformPoint(default);
		NativeMethods.UpdatePosition(p.X, p.Y);
	}

	public void InvalidateLayout()
	{
		UpdateSize();
		UpdatePosition();
	}

	public void SetText(string text)
	{
		if (!_view.TextBox?.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
			return;
		}
		NativeMethods.SetText(text);
	}

	public void Select(int start, int length)
	{
		if (!_view.TextBox?.IsFocused ?? true)
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
		if (!_view.TextBox?.IsFocused ?? true)
		{
			// The invisible <input /> instance is shared between all TextBoxes, so only propagate state from managed to native
			// when this TextBox is the one in focus
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
		if (_view?.TextBox is { } textBox)
		{
			return TextBoxExtensions.GetInputReturnType(textBox).ToEnterKeyHintValue();
		}

		return "";
	}

	private string GetInputModeValue()
	{
		if (_view?.TextBox is { } textBox)
		{
			return textBox.InputScope.ToInputModeValue();
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
		public static partial void Focus(bool isPassword, string? text, bool acceptsReturn, string inputMode, string enterKeyHint, bool isSpellCheckEnabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.blur")]
		public static partial void Blur();

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
