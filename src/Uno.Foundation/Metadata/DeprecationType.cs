namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates a deprecation as Deprecate or Remove.
/// </summary>
public enum DeprecationType
{
	/// <summary>
	/// Compilers and other tools should treat the entity
	/// as deprecated. This is the default.
	/// </summary>
	Deprecate = 0,

	/// <summary>
	/// Compilers and other tools should treat the entity as removed.
	/// </summary>
	Remove = 1,
}
