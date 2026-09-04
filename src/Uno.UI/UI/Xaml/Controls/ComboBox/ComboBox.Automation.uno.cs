#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
	internal string GetItemDisplayTextForAutomation(object? item)
	{
		if (item is null)
		{
			return string.Empty;
		}

		EnsurePropertyPathListener();
		return TryGetStringValue(item, m_spPropertyPathListener) ?? string.Empty;
	}
}
