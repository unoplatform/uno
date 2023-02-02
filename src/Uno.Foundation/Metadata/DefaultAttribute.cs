using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the default interface for a runtime class.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public partial class DefaultAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public DefaultAttribute() : base()
	{
	}
}
