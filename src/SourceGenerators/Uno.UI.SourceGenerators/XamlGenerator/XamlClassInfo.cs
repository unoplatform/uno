#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator;

/// <summary>
/// Represents parsed x:Class information from a XAML file, used for code-behind generation.
/// </summary>
internal readonly record struct XamlClassInfo(
	string FullClassName,
	string Namespace,
	string ClassName,
	string RootElementName,
	string RootElementNamespace,
	string BaseTypeFullName);
