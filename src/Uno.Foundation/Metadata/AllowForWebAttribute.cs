using System;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Enables developers to expose a native object as a global parameter in the context of the top-level document
/// inside of a WebView.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public partial class AllowForWebAttribute : Attribute
{
	public AllowForWebAttribute() : base()
	{
	}
}
