namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation client 
/// to controls that have an intrinsic value that does not span a range and that can 
/// be represented as a string. Implement this interface in order to support the capabilities 
/// that an automation client requests with a GetPattern call and PatternInterface.Value.
/// </summary>
public partial interface IValueProvider
{
	/// <summary>
	/// Gets a value that indicates whether the value of a control is read-only.
	/// </summary>
	bool IsReadOnly { get; }

	/// <summary>
	/// Gets the value of the control.
	/// </summary>
	string Value { get; }

	/// <summary>
	/// Sets the value of a control.
	/// </summary>
	/// <param name="value">
	/// The value to set. The provider is responsible 
	/// for converting the value to the appropriate data type.
	/// </param>
	void SetValue(string value);
}
