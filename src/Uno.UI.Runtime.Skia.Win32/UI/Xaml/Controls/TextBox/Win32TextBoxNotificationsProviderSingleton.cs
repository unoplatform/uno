using Windows.Foundation;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class Win32TextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal static Win32TextBoxNotificationsProviderSingleton Instance { get; } = new();

	private Win32ImeCaretManager? _activeManager;

	private Win32TextBoxNotificationsProviderSingleton()
	{
	}

	public void OnFocused(TextBox textBox)
	{
		_activeManager?.Deactivate();
		_activeManager = null;

		if (!TryGetHwnd(textBox, out var hwnd))
		{
			return;
		}

		_activeManager = new Win32ImeCaretManager(hwnd);

		var (x, y) = GetCaretClientPixelPosition(textBox);
		_activeManager.Activate(x, y);
	}

	public void OnUnfocused(TextBox textBox)
	{
		_activeManager?.Deactivate();
		_activeManager = null;
	}

	public void OnEnteredVisualTree(TextBox textBox)
	{
	}

	public void OnLeaveVisualTree(TextBox textBox)
	{
	}

	public void FinishAutofillContext(bool shouldSave)
	{
	}

	public void NotifyValueChanged(TextBox textBox)
	{
		UpdateCaretPosition(textBox);
	}

	public void NotifySelectionChanged(TextBox textBox)
	{
		UpdateCaretPosition(textBox);
	}

	private void UpdateCaretPosition(TextBox textBox)
	{
		if (_activeManager is null)
		{
			return;
		}

		var (x, y) = GetCaretClientPixelPosition(textBox);
		_activeManager.UpdatePosition(x, y);
	}

	/// <summary>
	/// Computes the caret position in client-area physical pixels.
	/// </summary>
	private static (int x, int y) GetCaretClientPixelPosition(TextBox textBox)
	{
		var textBoxView = textBox.TextBoxView;
		if (textBoxView?.DisplayBlock?.ParsedText is null || textBox.XamlRoot is null)
		{
			return (0, 0);
		}

		// Get the character index at the caret position (caret is at SelectionStart for backward selections)
		var index = textBox.IsBackwardSelection ? textBox.SelectionStart : textBox.SelectionStart + textBox.SelectionLength;

		// Get the rect for the character at the caret position (in DisplayBlock-local DIPs)
		var rect = textBoxView.DisplayBlock.ParsedText.GetRectForIndex(index);

		// Transform from DisplayBlock coordinates to root coordinates (root DIPs = client DIPs)
		var transform = textBoxView.DisplayBlock.TransformToVisual(null);
		var point = transform.TransformPoint(new Point(rect.Left, rect.Top));

		// Convert from DIPs to client-area physical pixels
		var scale = textBox.XamlRoot.RasterizationScale;
		return ((int)(point.X * scale), (int)(point.Y * scale));
	}

	private static bool TryGetHwnd(TextBox textBox, out HWND hwnd)
	{
		hwnd = HWND.Null;

		if (textBox.XamlRoot is not { } xamlRoot)
		{
			return false;
		}

		if (XamlRootMap.GetHostForRoot(xamlRoot) is not Win32WindowWrapper wrapper)
		{
			return false;
		}

		if (wrapper.NativeWindow is not Win32NativeWindow nativeWindow)
		{
			return false;
		}

		hwnd = (HWND)nativeWindow.Hwnd;
		return true;
	}
}
