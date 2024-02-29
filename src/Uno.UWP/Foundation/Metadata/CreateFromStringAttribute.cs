using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Creates a metadata object from a string.
/// </summary>
public partial class CreateFromStringAttribute : Attribute
{
	/// <summary>
	/// Creates an instance of the attribute.
	/// </summary>
	public CreateFromStringAttribute() : base()
	{
	}

	/// <summary>
	/// Full name of the method performing conversion from string.
	/// </summary>
	public string MethodName = null!;
}
