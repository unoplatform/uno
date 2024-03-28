#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.Core.Comparison;
using Uno.Foundation.Runtime.WebAssembly.Interop;

namespace Windows.UI.Xaml;

internal static class HtmlElementHelper
{
	private static readonly Dictionary<Type, HtmlTag> _cache = new(FastTypeComparer.Default);
	private static readonly Type _htmlElementAttribute;
	private static readonly PropertyInfo _htmlElementAttributeTagGetter;
	private static readonly Assembly _unoUIAssembly = typeof(UIElement).Assembly;

	static HtmlElementHelper()
	{
		_htmlElementAttribute = GetUnoUIRuntimeWebAssembly().GetType("Uno.UI.Runtime.WebAssembly.HtmlElementAttribute", true)!;
		_htmlElementAttributeTagGetter = _htmlElementAttribute.GetProperty("Tag") ?? throw new InvalidOperationException("Failed to resolve Tag property on HtmlElementAttribute.");
	}

	private static Assembly GetUnoUIRuntimeWebAssembly()
	{
		const string UnoUIRuntimeWebAssemblyName = "Uno.UI.Runtime.WebAssembly";

		// .NET Core fails to load assemblies property because of ALC issues: https://github.com/dotnet/runtime/issues/44269
		return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == UnoUIRuntimeWebAssemblyName)
			?? throw new InvalidOperationException($"Unable to find {UnoUIRuntimeWebAssemblyName} in the loaded assemblies");
	}

	internal static HtmlTag GetHtmlTag(Type type, string defaultHtmlTag)
	{
		if (type.Assembly == _unoUIAssembly)
		{
			return new HtmlTag(defaultHtmlTag, IsExternallyDefined: false);
		}

		if (_cache.TryGetValue(type, out var tag))
		{
			return tag;
		}

		tag = type.GetCustomAttribute(_htmlElementAttribute, true) is Attribute attr
			&& _htmlElementAttributeTagGetter.GetValue(attr, Array.Empty<object>()) is string tagName
			? new HtmlTag(tagName, IsExternallyDefined: true)
			: new HtmlTag(defaultHtmlTag, IsExternallyDefined: false);

		_cache[type] = tag;

		return tag;
	}

	/// <summary>
	/// Info about the tag to use in the DOM for a UI element.
	/// </summary>
	/// <param name="Name">The name of the tag used in the DOM</param>
	/// <param name="IsExternallyDefined">
	/// Indicates if this tag is not the default one that has been defined in the Uno assembly
	/// (using the HtmlElementAttribute).
	/// </param>
	internal record struct HtmlTag(string Name, bool IsExternallyDefined);
}
