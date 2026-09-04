using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia;

internal sealed class BrowserSkiaTextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal static BrowserSkiaTextBoxNotificationsProviderSingleton Instance { get; } = new();

	private BrowserSkiaTextBoxNotificationsProviderSingleton()
	{
	}

	public void OnFocused(TextBoxCore core) => SyncTextBox(core);

	public void OnUnfocused(TextBoxCore core)
	{
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

	public void NotifyValueChanged(TextBoxCore core) => SyncTextBox(core);

	public void NotifySelectionChanged(TextBoxCore core) => SyncTextBox(core);

	private static void SyncTextBox(TextBoxCore core)
		=> WebAssemblyAccessibility.Instance.SyncTextBoxValueAndSelection(core);
}
