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

	public void OnFocused(TextBoxCore core) => X11ImeTextBoxExtension.Instance.UpdateSpotLocationFromTextBox(core);

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

	public void NotifyValueChanged(TextBoxCore core) => X11ImeTextBoxExtension.Instance.UpdateSpotLocationFromTextBox(core);

	public void NotifySelectionChanged(TextBoxCore core) => X11ImeTextBoxExtension.Instance.UpdateSpotLocationFromTextBox(core);
}
