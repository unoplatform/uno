using System;

namespace Windows.UI.Xaml.Markup
{
	/// <summary>
	/// Represents a service that resolves from named elements in XAML markup to the appropriate CLR type.
	/// </summary>
	public partial interface IXamlTypeResolver
	{
		/// <summary>
		/// Resolves a named XAML type to the corresponding CLR Type.
		/// </summary>
		/// <param name="qualifiedTypeName">
		/// The XAML type name to resolve. The type name is optionally qualified
		/// by the prefix for a XML namespace. Otherwise the current default
		/// XML namespace is assumed.
		/// </param>
		/// <returns>The Type that qualifiedTypeName resolves to.</returns>
		Type Resolve(string qualifiedTypeName);
	}
}
