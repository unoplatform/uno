using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia;

internal sealed class BrowserSkiaTextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal static BrowserSkiaTextBoxNotificationsProviderSingleton Instance { get; } = new();

	private BrowserSkiaTextBoxNotificationsProviderSingleton()
	{
	}

	public void OnFocused(TextBox textBox) => SyncTextBox(textBox);

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

	public void NotifyValueChanged(TextBox textBox) => SyncTextBox(textBox);

	public void NotifySelectionChanged(TextBox textBox) => SyncTextBox(textBox);

	private static void SyncTextBox(TextBox textBox)
		=> WebAssemblyAccessibility.Instance.SyncTextBoxValueAndSelection(textBox);
}
