namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines values that specify the input validation behavior of a NumberBox when invalid input is entered.
/// </summary>
public enum NumberBoxValidationMode
{
	/// <summary>
	/// Invalid input is replaced by NumberBox.PlaceholderText text.
	/// </summary>
	InvalidInputOverwritten = 0,

	/// <summary>
	/// Input validation is disabled.
	/// </summary>
	Disabled = 1
};
