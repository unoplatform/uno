using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that a method is the default overload method.
/// This attribute must be used with OverloadAttribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public partial class DefaultOverloadAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public DefaultOverloadAttribute() : base()
	{
	}
}
