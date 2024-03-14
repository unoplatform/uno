using System.Runtime.InteropServices.JavaScript;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
namespace Uno.UI.Runtime.Skia;

internal partial class BrowserInvisibleTextBoxViewExtension: IOverlayTextBoxViewExtension
{
	private static readonly Point Zero = new Point(0, 0);

	private readonly TextBoxView _view;
	private readonly string _inputId;

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.createInput")]
	[return: JSMarshalAs<JSType.String>]
	private static partial string CreateInput([JSMarshalAs<JSType.Any>] object instance, bool isPasswordBox);

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.disposeInput")]
	private static partial void DisposeInput(string id);

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.initialize")]
	private static partial void Initialize();

	public BrowserInvisibleTextBoxViewExtension(TextBoxView textBoxView)
	{
		_view = textBoxView;
		_inputId = CreateInput(this, textBoxView.IsPasswordBox);

		Initialize();
	}

	~BrowserInvisibleTextBoxViewExtension()
	{
		DisposeInput(_inputId);
	}

	[JSExport]
	private static void OnInputTextChanged([JSMarshalAs<JSType.Any>] object @this, string text)
		=> ((BrowserInvisibleTextBoxViewExtension)@this)._view.UpdateTextFromNative(text);

	// The "overlay layer" is the DOM, which is always present.
	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.focus")]
	private static partial void Focus(string id, bool focused);

	public void StartEntry() => Focus(_inputId, true);
	public void EndEntry() => Focus(_inputId, false);

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updateSize")]
	private static partial void UpdateSize(string id, double width, double height);

	public void UpdateSize() => UpdateSize(_inputId, _view.DisplayBlock.ActualWidth, _view.DisplayBlock.ActualHeight);

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.updatePosition")]
	private static partial void UpdatePosition(string id, double x, double y);

	public void UpdatePosition()
	{
		var p = _view.DisplayBlock.TransformToVisual(null).TransformPoint(Zero);
		UpdatePosition(_inputId, p.X, p.Y);
	}

	public void InvalidateLayout()
	{
		UpdateSize();
		UpdatePosition();
	}

	[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.setText")]
	private static partial void SetText(string id, string text);

	public void SetText(string text) => SetText(_inputId, text);

	// Since we don't actually use the <input /> visually, do we don't need to take care of any of the visual aspects
	public void UpdateNativeView() { }
	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }
	public void Select(int start, int length) { }
	public void UpdateProperties() { }
	public int GetSelectionStart() => 0;
	public int GetSelectionLength() => 0;
	public int GetSelectionStartBeforeKeyDown() => 0;
	public int GetSelectionLengthBeforeKeyDown() => 0;
}
