using System.Collections.Generic;
using System.Runtime.InteropServices;
using Android.Text;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaTextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal List<TextBox> LiveTextBoxes { get; } = new();
	internal Dictionary<int, TextBox> LiveTextBoxesMap { get; } = new();

	public static AndroidSkiaTextBoxNotificationsProviderSingleton Instance { get; } = new AndroidSkiaTextBoxNotificationsProviderSingleton();

	private AndroidSkiaTextBoxNotificationsProviderSingleton()
	{
	}

	public void OnFocused(TextBox textBox)
	{
		if (UnoSKCanvasView.Instance is { } canvasView)
		{
			canvasView.TextInputPlugin.ShowTextInput(InputTypes.TextVariationNormal/*TODO: Properly provide this based on textBox.InputScope. This should also respect AcceptsReturn and IsSpellCheckEnabled*/);
			canvasView.TextInputPlugin.NotifyViewEntered(textBox, textBox.GetHashCode());
		}
	}

	public void OnUnfocused(TextBox textBox)
	{
		if (UnoSKCanvasView.Instance is { } canvasView)
		{
			canvasView.TextInputPlugin.HideTextInput();
			canvasView.TextInputPlugin.NotifyViewExited(textBox.GetHashCode());
		}
	}

	public void OnEnteredVisualTree(TextBox textBox)
	{
		LiveTextBoxes.Add(textBox);
		LiveTextBoxesMap.Add(textBox.GetHashCode(), textBox);
	}

	public void OnLeaveVisualTree(TextBox textBox)
	{
		LiveTextBoxes.Remove(textBox);
		LiveTextBoxesMap.Remove(textBox.GetHashCode());
	}

	public void FinishAutofillContext(bool shouldSave)
	{
		if (UnoSKCanvasView.Instance is { } canvasView)
		{
			canvasView.TextInputPlugin.FinishAutofillContext(shouldSave);
		}
	}

	public void NotifyValueChanged(TextBox textBox)
	{
		if (UnoSKCanvasView.Instance is { } canvasView)
		{
			canvasView.TextInputPlugin.NotifyValueChanged(textBox.GetHashCode(), textBox.Text);
		}
	}
}
