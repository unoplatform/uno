#nullable enable

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator;

/// <summary>
/// Lightweight XAML parser for the standalone WinUI code-behind generator path.
/// Extracts x:Class and root element information using XDocument.
/// </summary>
internal static class XamlCodeBehindParser
{
	private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
	private static readonly XNamespace DefaultNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

	/// <summary>
	/// Maps common WinUI root element names (in the default XAML namespace) to fully-qualified type names.
	/// </summary>
	private static readonly Dictionary<string, string> WinUIRootElementMap = new(StringComparer.Ordinal)
	{
		["Page"] = "Microsoft.UI.Xaml.Controls.Page",
		["UserControl"] = "Microsoft.UI.Xaml.Controls.UserControl",
		["Window"] = "Microsoft.UI.Xaml.Window",
		["Application"] = "Microsoft.UI.Xaml.Application",
		["ResourceDictionary"] = "Microsoft.UI.Xaml.ResourceDictionary",
		["ContentDialog"] = "Microsoft.UI.Xaml.Controls.ContentDialog",
	};

	/// <summary>
	/// Parses a XAML file's content and extracts x:Class and root element information.
	/// Returns null if the XAML has no x:Class attribute.
	/// </summary>
	/// <param name="xamlContent">The XAML file content as a string.</param>
	/// <param name="errorMessage">An error message if x:Class is malformed, null otherwise.</param>
	/// <returns>A <see cref="XamlClassInfo"/> if x:Class is present and valid, null otherwise.</returns>
	public static XamlClassInfo? Parse(string xamlContent, out string? errorMessage)
	{
		errorMessage = null;

		XDocument doc;
		try
		{
			doc = XDocument.Parse(xamlContent);
		}
		catch (Exception)
		{
			// Malformed XML - let the XAML compiler report the error
			return null;
		}

		if (doc.Root is null)
		{
			return null;
		}

		var xClassAttribute = doc.Root.Attribute(XamlNamespace + "Class");
		if (xClassAttribute is null)
		{
			return null;
		}

		var xClassValue = xClassAttribute.Value;
		var lastDot = xClassValue.LastIndexOf('.');
		if (lastDot <= 0)
		{
			errorMessage = $"Invalid x:Class value '{xClassValue}' - must include namespace";
			return null;
		}

		var ns = xClassValue.Substring(0, lastDot);
		var className = xClassValue.Substring(lastDot + 1);

		if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(className))
		{
			errorMessage = $"Invalid x:Class value '{xClassValue}' - must include namespace and class name";
			return null;
		}
		var rootElementName = doc.Root.Name.LocalName;
		var rootElementNamespace = doc.Root.Name.NamespaceName;

		var baseTypeFullName = ResolveBaseType(rootElementName, rootElementNamespace);

		return new XamlClassInfo(
			FullClassName: xClassValue,
			Namespace: ns,
			ClassName: className,
			RootElementName: rootElementName,
			RootElementNamespace: rootElementNamespace,
			BaseTypeFullName: baseTypeFullName);
	}

	/// <summary>
	/// Well-known WinUI CLR namespaces to search when resolving default-namespace XAML elements
	/// via compilation metadata. These match the PresentationNamespaces from XamlConstants.
	/// </summary>
	private static readonly string[] WinUIPresentationNamespaces =
	{
		"Microsoft.UI.Xaml.Controls",
		"Microsoft.UI.Xaml.Controls.Primitives",
		"Microsoft.UI.Xaml.Shapes",
		"Microsoft.UI.Xaml.Input",
		"Microsoft.UI.Xaml.Media",
		"Microsoft.UI.Xaml.Media.Animation",
		"Microsoft.UI.Xaml.Media.Imaging",
		"Windows.UI",
		"Microsoft.UI.Xaml",
		"Microsoft.UI.Xaml.Data",
		"Microsoft.UI.Xaml.Documents",
		"Windows.UI.Text",
		"Microsoft.UI.Xaml.Automation",
		"System",
	};

	/// <summary>
	/// Resolves the fully-qualified base type name from the root element name and namespace.
	/// Returns null if the type cannot be resolved.
	/// </summary>
	private static string? ResolveBaseType(string rootElementName, string rootElementNamespace)
	{
		// Default WinUI namespace elements — fast path for the 6 most common root types
		if (string.Equals(rootElementNamespace, DefaultNamespace.NamespaceName, StringComparison.Ordinal)
			&& WinUIRootElementMap.TryGetValue(rootElementName, out var knownType))
		{
			return knownType;
		}

		// For unknown types in the default namespace, return null (caller must resolve via compilation)
		if (string.Equals(rootElementNamespace, DefaultNamespace.NamespaceName, StringComparison.Ordinal))
		{
			return null;
		}

		// For CLR namespace-based xmlns (e.g., "using:MyApp.Controls" or "clr-namespace:MyApp.Controls"),
		// try to resolve the full type name
		foreach (var prefix in new[] { "using:", "clr-namespace:" })
		{
			var parts = rootElementNamespace.Split(new[] { prefix }, 2, StringSplitOptions.None);
			if (parts.Length == 2)
			{
				var xmlns = parts[1].Split(';')[0];
				return $"{xmlns}.{rootElementName}";
			}
		}

		// Unknown namespace URI — cannot resolve
		return null;
	}

	/// <summary>
	/// Resolves a default-namespace XAML element type using compilation metadata.
	/// Probes well-known WinUI CLR namespaces to find the type.
	/// </summary>
	/// <param name="compilation">The current compilation.</param>
	/// <param name="rootElementName">The local name of the root XAML element (e.g. "Button", "Grid").</param>
	/// <returns>The fully-qualified metadata name if found, null otherwise.</returns>
	public static string? ResolveBaseTypeFromCompilation(Compilation compilation, string rootElementName)
	{
		foreach (var ns in WinUIPresentationNamespaces)
		{
			var fullName = ns + "." + rootElementName;
			if (compilation.GetTypeByMetadataName(fullName) is not null)
			{
				return fullName;
			}
		}

		return null;
	}

	/// <summary>
	/// Determines whether code-behind generation should occur for a file, based on
	/// per-file metadata and project-level property with proper precedence.
	/// </summary>
	/// <param name="perFileValue">Per-file UnoGenerateCodeBehind metadata value (null if not set).</param>
	/// <param name="globalValue">Project-level UnoGenerateCodeBehind property value (null if not set).</param>
	/// <returns>True if code-behind generation should occur.</returns>
	public static bool ShouldGenerateCodeBehind(string? perFileValue, string? globalValue)
	{
		// Per-file metadata takes precedence (FR-009)
		if (!string.IsNullOrEmpty(perFileValue))
		{
			return string.Equals(perFileValue, "true", StringComparison.OrdinalIgnoreCase);
		}

		// Fall back to project-level property (FR-007: default is true)
		if (!string.IsNullOrEmpty(globalValue))
		{
			return string.Equals(globalValue, "true", StringComparison.OrdinalIgnoreCase);
		}

		// Default: enabled (FR-007)
		return true;
	}
}
