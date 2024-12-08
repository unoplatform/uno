namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Defines the type of text edit change.
/// </summary>
public enum AutomationTextEditChangeType
{
	/// <summary>
	/// Not related to a specific change type.
	/// </summary>
	None = 0,

	/// <summary>
	/// Change is from an auto-correct action performed by a control.
	/// </summary>
	AutoCorrect = 1,

	/// <summary>
	/// Change is from an IME active composition within a control.
	/// </summary>
	Composition = 2,

	/// <summary>
	/// Change is from an IME composition going from active to finalized state within a control.
	/// </summary>
	CompositionFinalized = 3,
}
