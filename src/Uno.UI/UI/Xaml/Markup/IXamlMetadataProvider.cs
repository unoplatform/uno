using System;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Markup;

/// <summary>
/// Implements XAML schema context concepts that support XAML parsing.
/// </summary>
public partial interface IXamlMetadataProvider
{
	/// <summary>
	/// Implements XAML schema context access to underlying type mapping, based on providing a helper value that describes a type.
	/// </summary>
	/// <param name="type">The type as represented by the relevant type system or interoperation support type.</param>
	/// <returns>The schema context's implementation of the IXamlType concept.</returns>
	IXamlType GetXamlType(Type type);

	/// <summary>
	/// Implements XAML schema context access to underlying type mapping, based on specifying a full type name.
	/// </summary>
	/// <param name="fullName">The name of the class for which to return a XAML type mapping.</param>
	/// <returns>The schema context's implementation of the IXamlType concept.</returns>
	IXamlType GetXamlType(string fullName);

	/// <summary>
	/// Gets the set of XMLNS (XAML namespace) definitions that apply to the context.
	/// </summary>
	/// <returns>The set of XMLNS (XAML namespace) definitions.</returns>
	XmlnsDefinition[] GetXmlnsDefinitions();
}
