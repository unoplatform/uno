using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls.Extensions;

internal interface ITextBoxNotificationsProviderSingleton
{
	void OnFocused(TextBoxCore core);

	void OnUnfocused(TextBoxCore core);

	void OnEnteredVisualTree(TextBoxCore core);

	void OnLeaveVisualTree(TextBoxCore core);

	void FinishAutofillContext(bool shouldSave);

	void NotifyValueChanged(TextBoxCore core);

	void NotifySelectionChanged(TextBoxCore core);
}
