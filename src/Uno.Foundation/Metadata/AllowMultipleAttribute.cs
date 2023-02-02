using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that multiple instances of a custom attribute can be applied to a target.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public partial class AllowMultipleAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public AllowMultipleAttribute() : base()
	{
	}
}
