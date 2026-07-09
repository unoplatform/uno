#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Uno.Core.Comparison;
using Uno.Foundation.Runtime.WebAssembly.Interop;

namespace Microsoft.UI.Xaml;

internal static class HtmlElementHelper
{
	private static readonly Dictionary<Type, HtmlTag> _cache = new(FastTypeComparer.Default);
	private static readonly Type _htmlElementAttribute;
	private static readonly PropertyInfo _htmlElementAttributeTagGetter;
	private static readonly Assembly _unoUIAssembly = typeof(UIElement).Assembly;

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "HtmlElementAttribute is suppressed from the linker")]
	[UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "HtmlElementAttribute is suppressed from the linker")]
	static HtmlElementHelper()
	{
		_htmlElementAttribute = GetUnoUIRuntimeWebAssembly().GetType("Uno.UI.Runtime.WebAssembly.HtmlElementAttribute", true)!;
		_htmlElementAttributeTagGetter = _htmlElementAttribute.GetProperty("Tag") ?? throw new InvalidOperationException("Failed to resolve Tag property on HtmlElementAttribute.");
	}

	private static Assembly GetUnoUIRuntimeWebAssembly()
	{
		const string UnoUIRuntimeWebAssemblyName = "Uno.UI.Runtime.WebAssembly";

		// .NET Core fails to load assemblies property because of ALC issues: https://github.com/dotnet/runtime/issues/44269
		return Uno.UI.Helpers.ContextualAssemblyResolver.GetRelevantAssemblies().FirstOrDefault(a => a.GetName().Name == UnoUIRuntimeWebAssemblyName)
			?? throw new InvalidOperationException($"Unable to find {UnoUIRuntimeWebAssemblyName} in the loaded assemblies");
	}

	/// <summary>
	/// Removes cache entries whose key <see cref="Type"/> belongs to a non-default (collectible)
	/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>. A downstream host that loads
	/// previewed apps into their own collectible AssemblyLoadContexts creates elements of the app's
	/// (external) control types; each is cached here, keeping the app's <see cref="Type"/> — and
	/// thus its context — alive for the process lifetime. Entries are re-cached on demand. Called
	/// from the ALC cleanup hook.
	/// </summary>
	internal static void ClearNonDefaultAlcEntries()
	{
		var defaultAlc = System.Runtime.Loader.AssemblyLoadContext.Default;
		List<Type>? keysToRemove = null;

		foreach (var key in _cache.Keys)
		{
			if (key.IsCollectible)
			{
				(keysToRemove ??= new List<Type>()).Add(key);
				continue;
			}

			var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(key.Assembly);
			if (alc is not null && alc != defaultAlc)
			{
				(keysToRemove ??= new List<Type>()).Add(key);
			}
		}

		if (keysToRemove is null)
		{
			return;
		}

		foreach (var key in keysToRemove)
		{
			_cache.Remove(key);
		}
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
