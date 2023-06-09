namespace Windows.Foundation;

/// <summary>
/// Provides a way to represent the current object as a string.
/// </summary>
public partial interface IStringable
{
	/// <summary>
	/// Gets a string that represents the current object.
	/// </summary>
	/// <returns>A string that represents the current object.</returns>
	string ToString();
}
