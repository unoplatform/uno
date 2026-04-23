#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// X11 implementation of <see cref="ITextBoxNotificationsProviderSingleton"/>.
/// Updates the XIM candidate window position when the caret moves.
/// </summary>
internal sealed class X11TextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal static X11TextBoxNotificationsProviderSingleton Instance { get; } = new();

	private X11TextBoxNotificationsProviderSingleton()
	{
	}

	public void OnFocused(TextBox textBox)
	{
		UpdateSpotLocation(textBox);
	}

	public void OnUnfocused(TextBox textBox)
	{
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
		UpdateSpotLocation(textBox);
	}

	public void NotifySelectionChanged(TextBox textBox)
	{
		UpdateSpotLocation(textBox);
	}

	private static void UpdateSpotLocation(TextBox textBox)
	{
		var textBoxView = textBox.TextBoxView;
		if (textBoxView?.DisplayBlock?.ParsedText is null || textBox.XamlRoot is null)
		{
			return;
		}

		var index = textBox.IsBackwardSelection ? textBox.SelectionStart : textBox.SelectionStart + textBox.SelectionLength;
		var rect = textBoxView.DisplayBlock.ParsedText.GetRectForIndex(index);
		var transform = textBoxView.DisplayBlock.TransformToVisual(null);
		var point = transform.TransformPoint(new Point(rect.Left, rect.Top + rect.Height));
		var scale = textBox.XamlRoot.RasterizationScale;

		X11ImeTextBoxExtension.Instance.UpdateSpotLocation(
			(int)(point.X * scale),
			(int)(point.Y * scale));
	}
}
