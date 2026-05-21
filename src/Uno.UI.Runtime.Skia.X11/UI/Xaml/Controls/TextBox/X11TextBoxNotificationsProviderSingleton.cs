#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// X11 implementation of <see cref="ITextBoxNotificationsProviderSingleton"/>.
/// Forwards caret/selection movement to <see cref="X11ImeTextBoxExtension"/> so the
/// active IME's candidate window tracks the caret.
/// </summary>
internal sealed class X11TextBoxNotificationsProviderSingleton : ITextBoxNotificationsProviderSingleton
{
	internal static X11TextBoxNotificationsProviderSingleton Instance { get; } = new();

	private X11TextBoxNotificationsProviderSingleton()
	{
	}

	public void OnFocused(TextBox textBox) => X11ImeTextBoxExtension.Instance.UpdateSpotLocationFromTextBox(textBox);

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

	public void NotifyValueChanged(TextBox textBox) => X11ImeTextBoxExtension.Instance.UpdateSpotLocationFromTextBox(textBox);

	public void NotifySelectionChanged(TextBox textBox) => X11ImeTextBoxExtension.Instance.UpdateSpotLocationFromTextBox(textBox);
}
