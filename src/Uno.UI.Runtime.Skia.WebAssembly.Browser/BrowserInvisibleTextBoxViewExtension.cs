using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserInvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private static TextBoxView? _view;
	private static BrowserInvisibleTextBoxViewExtension? _instance;

	private BrowserInvisibleTextBoxViewExtension()
	{
		NativeMethods.Initialize();
	}

	public static BrowserInvisibleTextBoxViewExtension Instance => _instance ??= new();

	[MemberNotNullWhen(returnValue: false, nameof(_view))]
	private static bool WarnOnNullView()
	{
		if (_view is null)
		{
			if (typeof(BrowserInvisibleTextBoxViewExtension).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(BrowserInvisibleTextBoxViewExtension).Log().LogWarning("UpdatePosition is called while _view is null.");
			}

			return true;
		}

		return false;
	}

	[JSExport]
	private static void OnInputTextChanged(string text)
		=> _view?.UpdateTextFromNative(text);

	// The "overlay layer" is the DOM, which is always present.
	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry(bool isPasswordBox, TextBoxView view)
	{
		_view = view;
		NativeMethods.Focus(true, isPasswordBox, view.TextBox?.Text);
	}

	public void EndEntry()
	{
		if (_view is not null)
		{
			_view = null;
			NativeMethods.Focus(false, default, default);
		}
	}

	public void UpdateSize()
	{
		if (WarnOnNullView())
		{
			return;
		}

		NativeMethods.UpdateSize(_view.DisplayBlock.ActualWidth, _view.DisplayBlock.ActualHeight);
	}

	public void UpdatePosition()
	{
		if (WarnOnNullView())
		{
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
		if (_view is not null)
		{
			NativeMethods.SetText(text);
		}
	}

	// Since we don't actually use the <input /> visually, do we don't need to take care of any of the visual aspects
	public void UpdateNativeView() { }
	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }
	public void Select(int start, int length) { }
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
	}
}
