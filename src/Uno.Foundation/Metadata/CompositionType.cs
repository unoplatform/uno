namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies the visibility of a programming element
/// for which the composable attribute is applied.
/// </summary>
public enum CompositionType
{
	/// <summary>
	/// Indicates that access to the programming element is limited to other
	/// elements in the containing class or types derived from the containing class.
	/// </summary>
	Protected = 1,

	/// <summary>
	/// Indicates that access to the programming element is not restricted.
	/// </summary>
	Public = 2,
}
