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

	public void OnFocused(TextBoxCore core)
	{
		_activeManager?.Deactivate();
		_activeManager = null;

		if (!TryGetHwnd(core, out var hwnd))
		{
			return;
		}

		_activeManager = new Win32ImeCaretManager(hwnd);

		var (x, y) = GetCaretClientPixelPosition(core);
		_activeManager.Activate(x, y);
	}

	public void OnUnfocused(TextBoxCore core)
	{
		_activeManager?.Deactivate();
		_activeManager = null;
	}

	public void OnEnteredVisualTree(TextBoxCore core)
	{
	}

	public void OnLeaveVisualTree(TextBoxCore core)
	{
	}

	public void FinishAutofillContext(bool shouldSave)
	{
	}

	public void NotifyValueChanged(TextBoxCore core)
	{
		UpdateCaretPosition(core);
	}

	public void NotifySelectionChanged(TextBoxCore core)
	{
		UpdateCaretPosition(core);
	}

	private void UpdateCaretPosition(TextBoxCore core)
	{
		if (_activeManager is null)
		{
			return;
		}

		var (x, y) = GetCaretClientPixelPosition(core);
		_activeManager.UpdatePosition(x, y);
	}

	/// <summary>
	/// Computes the caret position in client-area physical pixels.
	/// </summary>
	private static (int x, int y) GetCaretClientPixelPosition(TextBoxCore core)
	{
		var textBoxView = core.TextBoxView;
		if (textBoxView?.DisplayBlock?.ParsedText is null || core.Owner.XamlRoot is null)
		{
			return (0, 0);
		}

		// Get the character index at the caret position (caret is at SelectionStart for backward selections)
		var index = core.IsBackwardSelection ? core.SelectionStart : core.SelectionStart + core.SelectionLength;

		// Get the rect for the character at the caret position (in DisplayBlock-local DIPs)
		var rect = textBoxView.DisplayBlock.ParsedText.GetRectForIndex(index);

		// Transform from DisplayBlock coordinates to root coordinates (root DIPs = client DIPs)
		var transform = textBoxView.DisplayBlock.TransformToVisual(null);
		var point = transform.TransformPoint(new Point(rect.Left, rect.Top));

		// Convert from DIPs to client-area physical pixels
		var scale = core.Owner.XamlRoot.RasterizationScale;
		return ((int)(point.X * scale), (int)(point.Y * scale));
	}

	private static bool TryGetHwnd(TextBoxCore core, out HWND hwnd)
	{
		hwnd = HWND.Null;

		if (core.Owner.XamlRoot is not { } xamlRoot)
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
