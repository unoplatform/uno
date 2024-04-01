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

	[JSExport]
	private static void OnInputTextChanged(string text)
	{
		var xamlRoot = WebAssemblyWindowWrapper.Instance.XamlRoot;
		// This only fires from password manager autocompletion, which is only available when the TextBox is focused
		var focusedElement = FocusManager.GetFocusedElement(xamlRoot!) as TextBox;
		focusedElement?.TextBoxView.UpdateTextFromNative(text);
	}

	// The "overlay layer" is the DOM, which is always present.
	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry()
	{
		NativeMethods.Focus(true, _view.IsPasswordBox, _view.TextBox?.Text);
		NativeMethods.UpdateSelection(_view.TextBox?.SelectionStart ?? 0, _view.TextBox?.SelectionLength ?? 0);
	}

	public void EndEntry() => NativeMethods.Focus(false, _view.IsPasswordBox, _view.TextBox?.Text);

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

	public void Select(int start, int length) => NativeMethods.UpdateSelection(start, length);

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
		public static partial void Focus(bool focused, bool isPassword, string? text);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updateSize")]
		public static partial void UpdateSize(double width, double height);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updatePosition")]
		public static partial void UpdatePosition(double x, double y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updateSelection")]
		public static partial void UpdateSelection(int start, int length);
	}
}
