namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values that specify whether a text provider supports selection 
/// and, if so, whether it supports a single, continuous selection 
/// or multiple, disjoint selections.
/// </summary>
public enum SupportedTextSelection
{
	/// <summary>
	/// Does not support text selections.
	/// </summary>
	None = 0,

	/// <summary>
	/// Supports a single, continuous text selection.
	/// </summary>
	Single = 1,

	/// <summary>
	/// Supports multiple, disjoint text selections.
	/// </summary>
	Multiple = 2,
}
