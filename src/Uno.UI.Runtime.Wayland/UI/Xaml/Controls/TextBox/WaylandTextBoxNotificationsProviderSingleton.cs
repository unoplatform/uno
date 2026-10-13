#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Wayland;

/// <summary>
/// Forwards caret, selection and text changes to <see cref="WaylandImeTextBoxExtension"/>
/// so the input method sees current surrounding text and places its popup at the caret.
/// </summary>
internal sealed class WaylandTextBoxNotificationsProviderSingleton(WaylandImeTextBoxExtension ime) : ITextBoxNotificationsProviderSingleton
{
	public void OnFocused(TextBoxCore core) => ime.UpdateState(core);

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

	public void NotifyValueChanged(TextBoxCore core) => ime.UpdateState(core);

	public void NotifySelectionChanged(TextBoxCore core) => ime.UpdateState(core);
}
