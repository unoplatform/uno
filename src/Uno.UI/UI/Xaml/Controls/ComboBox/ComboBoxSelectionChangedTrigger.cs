namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify what action causes a SelectionChanged event to occur.
/// </summary>
public enum ComboBoxSelectionChangedTrigger
{
	/// <summary>
	/// A change event occurs when the user commits a selection in the combo box.
	/// </summary>
	Committed = 0,

	/// <summary>
	/// A change event occurs each time the user navigates to a new selection in the combo box.
	/// </summary>
	Always = 1,
}
