#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Uno.Core.Comparison;
using Uno.Foundation.Logging;
using Uno.Foundation.Runtime.WebAssembly.Interop;

namespace Microsoft.UI.Xaml;

internal static class HtmlElementHelper
{
	// Guarded by _cacheGate: lookups happen on the UI thread, but the ALC teardown sweep can reach
	// this cache from other teardown paths, and Dictionary corrupts under concurrent mutation.
	private static readonly Dictionary<Type, HtmlTag> _cache = new(FastTypeComparer.Default);
	private static readonly object _cacheGate = new();
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
		int removed;
		lock (_cacheGate)
		{
			removed = Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(_cache);
		}

		if (removed > 0 && typeof(HtmlElementHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			typeof(HtmlElementHelper).Log().Debug($"[ALC-CLEANUP] HtmlElementHelper: removed {removed} non-default-ALC tag cache entrie(s).");
		}
	}

	internal static HtmlTag GetHtmlTag(Type type, string defaultHtmlTag)
	{
		if (type.Assembly == _unoUIAssembly)
		{
			return new HtmlTag(defaultHtmlTag, IsExternallyDefined: false);
		}

		lock (_cacheGate)
		{
			if (_cache.TryGetValue(type, out var tag))
			{
				return tag;
			}
		}

		// The attribute reflection runs outside the lock (it can be arbitrarily expensive); a
		// racing lookup for the same type would merely compute the same value twice.
		var computed = type.GetCustomAttribute(_htmlElementAttribute, true) is Attribute attr
			&& _htmlElementAttributeTagGetter.GetValue(attr, Array.Empty<object>()) is string tagName
			? new HtmlTag(tagName, IsExternallyDefined: true)
			: new HtmlTag(defaultHtmlTag, IsExternallyDefined: false);

		lock (_cacheGate)
		{
			_cache[type] = computed;
		}

		return computed;
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
