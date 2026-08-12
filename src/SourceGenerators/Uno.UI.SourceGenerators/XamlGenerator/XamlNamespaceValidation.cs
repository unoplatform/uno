#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Uno.UI.SourceGenerators.XamlGenerator;

/// <summary>
/// Rejection of the WPF-style <c>clr-namespace:</c> xmlns form, which WinUI does not support.
/// </summary>
internal static class XamlNamespaceValidation
{
	public const string ClrNamespacePrefix = "clr-namespace:";

	private const string MarkupCompatibilityNamespace = "http://schemas.openxmlformats.org/markup-compatibility/2006";

	private static readonly char[] _spaceArray = new[] { ' ', '\t', '\r', '\n' };

	public static bool IsClrNamespace(string @namespace)
		=> @namespace.StartsWith(ClrNamespacePrefix, StringComparison.Ordinal);

	public static string FormatUnsupportedClrNamespaceMessage(string prefix, string @namespace)
	{
		var declaration = prefix.Length > 0 ? $"xmlns:{prefix}" : "xmlns";
		var clrNamespace = @namespace.Substring(ClrNamespacePrefix.Length).Split(';')[0];

		return $"The 'clr-namespace:' XAML namespace form is not supported. Replace '{declaration}=\"{@namespace}\"' with '{declaration}=\"using:{clrNamespace}\"'";
	}

	/// <summary>
	/// Reads the <c>mc:Ignorable</c> prefixes declared on the root element. Only the root element is
	/// considered, which is where design-time tooling declares them.
	/// </summary>
	public static ICollection<string> GetRootIgnorablePrefixes(string content)
	{
		try
		{
			using var reader = XmlReader.Create(new StringReader(content));
			if (reader.MoveToContent() != XmlNodeType.Element)
			{
				return Array.Empty<string>();
			}

			if (reader.GetAttribute("Ignorable", MarkupCompatibilityNamespace) is { Length: > 0 } ignorable)
			{
				return new HashSet<string>(ignorable.Split(_spaceArray, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
			}
		}
		catch (XmlException)
		{
			// Malformed markup is reported by the main parsing pass.
		}

		return Array.Empty<string>();
	}
}
