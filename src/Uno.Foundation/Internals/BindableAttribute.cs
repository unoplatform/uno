#nullable enable

using System;

namespace Uno.Foundation.Internals
{
	/// <summary>
	/// Marks a type as bindable for the NAOT trimmer.
	/// This attribute is used by the BindableTypeProvidersSourceGenerator to identify types
	/// that should have binding metadata generated, preventing them from being trimmed.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
	internal sealed class BindableAttribute : Attribute
	{
	}
}
