using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
		NativeMethods.Focus(true, _view.IsPasswordBox, _view.TextBox?.Text, _view.TextBox?.AcceptsReturn ?? false);
		NativeMethods.UpdateSelection(_view.TextBox?.SelectionStart ?? 0, _view.TextBox?.SelectionLength ?? 0, SelectionDirection);
	}

	public void EndEntry() => NativeMethods.Focus(false, _view.IsPasswordBox, _view.TextBox?.Text, _view.TextBox?.AcceptsReturn ?? false);

	public void UpdateSize() => NativeMethods.UpdateSize(_view.DisplayBlock.ActualWidth, _view.DisplayBlock.ActualHeight);

	public void UpdatePosition()
	{
		var p = _view.DisplayBlock.TransformToVisual(null).TransformPoint(default);
		NativeMethods.UpdatePosition(p.X, p.Y);
	}

	public void InvalidateLayout()
	{
		UpdateSize();
		UpdatePosition();
	}

	public void SetText(string text) => NativeMethods.SetText(text);

	public void Select(int start, int length)
	{
		NativeMethods.UpdateSelection(start, length, SelectionDirection);
	}

	// Since we don't actually use the <input /> visually, do we don't need to take care of any of the visual aspects
	public void UpdateNativeView() { }
	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }
	public void UpdateProperties() { }
	public int GetSelectionStart() => 0;
	public int GetSelectionLength() => 0;
	public int GetSelectionStartBeforeKeyDown() => 0;
	public int GetSelectionLengthBeforeKeyDown() => 0;

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.initialize")]
		public static partial void Initialize();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setText")]
		public static partial void SetText(string text);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.focus")]
		public static partial void Focus(bool focused, bool isPassword, string? text, bool acceptsReturn);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updateSize")]
		public static partial void UpdateSize(double width, double height);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updatePosition")]
		public static partial void UpdatePosition(double x, double y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updateSelection")]
		public static partial void UpdateSelection(int start, int length, string direction);
	}
}
