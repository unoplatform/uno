#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Uno.UI.SourceGenerators.XamlGenerator;

/// <summary>
/// Validation of xmlns declarations: the WPF-style <c>clr-namespace:</c> form, which WinUI does not support,
/// and the conditional prefixes and <c>Uno.UI.Toolkit</c> namespace removed in Uno Platform 7.0.
/// </summary>
internal static class XamlNamespaceValidation
{
	public const string ClrNamespacePrefix = "clr-namespace:";

	private const string UsingNamespacePrefix = "using:";
	private const string LegacyPrefix = "legacy";
	private const string DropPrefixAdvice = "Drop the prefix from the markup using it.";

	private const string MarkupCompatibilityNamespace = "http://schemas.openxmlformats.org/markup-compatibility/2006";

	private static readonly char[] _spaceArray = new[] { ' ', '\t', '\r', '\n' };

	/// <summary>
	/// The conditional prefixes removed in 7.0, with the advice to migrate away from each of them.
	/// </summary>
	private static readonly Dictionary<string, string> _removedConditionalPrefixes = new(StringComparer.Ordinal)
	{
		["skia"] = UseInstead("not_winappsdk"),
		["netstdref"] = UseInstead("not_winappsdk"),
		["not_skia"] = UseInstead("winappsdk"),
		["not_netstdref"] = UseInstead("winappsdk"),
		["androidskia"] = UseInstead("android"),
		["not_androidskia"] = UseInstead("not_android"),
		["iosskia"] = UseInstead("ios"),
		["not_iosskia"] = UseInstead("not_ios"),
		["tvosskia"] = UseInstead("tvos"),
		["not_tvosskia"] = UseInstead("not_tvos"),
		["wasmskia"] = UseInstead("wasm"),
		["not_wasmskia"] = UseInstead("not_wasm"),
		["macos"] = "It named the native macOS head and has selected nothing since Uno Platform 5.0; remove it along with the markup using it. 'desktop' also covers Windows and Linux, so gate macOS-only content on OperatingSystem.IsMacOS() instead.",
		["not_macos"] = "It has applied on every target since Uno Platform 5.0; drop the prefix from the markup using it. 'not_desktop' is not an equivalent, since it also excludes Windows and Linux.",
		["not_mux"] = "Remove it along with the markup using it; it dates from UWP support and never applied.",
		["xamarin"] = DropPrefixAdvice,
		[LegacyPrefix] = DropPrefixAdvice,
	};

	/// <summary>
	/// The <c>Uno.UI.Toolkit</c> namespace removed in 7.0, whose types moved to the <c>Uno.UI.*</c> namespaces.
	/// </summary>
	private const string RemovedToolkitNamespace = UsingNamespacePrefix + "Uno.UI.Toolkit";

	public static bool IsRemovedToolkitNamespace(string @namespace)
		=> @namespace == RemovedToolkitNamespace;

	public static string FormatRemovedToolkitNamespaceMessage(string prefix)
	{
		var declaration = prefix.Length > 0 ? $"xmlns:{prefix}" : "xmlns";

		return $"The 'Uno.UI.Toolkit' namespace was removed in Uno Platform 7.0, so '{declaration}=\"{RemovedToolkitNamespace}\"' no longer resolves. "
			+ "Point it at the namespace of the types the markup uses: 'using:Uno.UI.Xaml.Controls' for ElevatedView, CommandBarExtensions and SplitViewExtensions, "
			+ "'using:Uno.UI.Behaviors' for VisibleBoundsPadding, 'using:Uno.UI.Xaml' for UIElementExtensions, 'using:Uno.UI.Xaml.Automation' for AutomationPropertiesExtensions, "
			+ "and 'using:Uno.UI.Xaml.Controls.Primitives' for ScrollBarExtensions. MenuFlyoutExtensions and MenuFlyoutItemExtensions were removed.";
	}

	public static bool IsClrNamespace(string @namespace)
		=> @namespace.StartsWith(ClrNamespacePrefix, StringComparison.Ordinal);

	/// <summary>
	/// Gets whether the declaration uses one of the conditional prefixes removed in 7.0. The same names remain
	/// valid as plain aliases of a <c>using:</c> namespace, and <c>legacy</c> is only reported where it acted
	/// as a condition, that is when it is listed in <c>mc:Ignorable</c>.
	/// </summary>
	public static bool IsRemovedConditionalPrefix(string prefix, string @namespace)
		=> _removedConditionalPrefixes.ContainsKey(prefix)
			&& (prefix == LegacyPrefix || !(IsClrNamespace(@namespace) || @namespace.StartsWith(UsingNamespacePrefix, StringComparison.Ordinal)));

	public static string? FormatRemovedConditionalPrefixMessage(string prefix, string @namespace, bool isIgnorable)
	{
		if (prefix == LegacyPrefix && !isIgnorable)
		{
			return null;
		}

		var effect = isIgnorable
			? "its content is now ignored on every target"
			: @namespace.Split('?')[0] == XamlConstants.PresentationXamlXmlNamespace
				? "its content now applies on every target"
				: "it is now an ordinary XML namespace";

		return $"The '{prefix}' conditional XAML prefix was removed in Uno Platform 7.0 and {effect}. {_removedConditionalPrefixes[prefix]}";
	}

	private static string UseInstead(string replacement)
		=> $"Use '{replacement}' instead.";

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
