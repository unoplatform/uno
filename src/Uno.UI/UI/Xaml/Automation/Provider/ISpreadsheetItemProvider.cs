namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Provides access to information about an item (cell) in a spreadsheet.
/// </summary>
public partial interface ISpreadsheetItemProvider
{
	/// <summary>
	/// Gets the formula for this spreadsheet cell, as a string.
	/// </summary>
	string Formula { get; }

	/// <summary>
	/// Returns an array of objects that represent the annotations associated with this spreadsheet cell.
	/// </summary>
	/// <returns>An array of IRawElementProviderSimple interfaces for Microsoft UI Automation elements 
	/// that represent the annotations associated with the spreadsheet cell.</returns>
	IRawElementProviderSimple[] GetAnnotationObjects();

	/// <summary>
	/// Returns an array of annotation type identifiers indicating the types of annotations
	/// that are associated with this spreadsheet cell.
	/// </summary>
	/// <returns>An array of annotation type identifiers, which contains one entry for each type of annotation 
	/// associated with the spreadsheet cell. For a list of possible values, see AnnotationType.</returns>
	AnnotationType[] GetAnnotationTypes();
}
