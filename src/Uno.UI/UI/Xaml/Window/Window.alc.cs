#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

partial class Window
{
	private static readonly ConditionalWeakTable<object, SecondaryAlcMarker> _secondaryAlcMarkers = new();

	private bool _isWindowFromSecondaryAlc;

	// Window-local storage to detect secondary ALC content
	private object? _secondaryAlcContent;

	partial void InitializeAlcState()
		=> _isWindowFromSecondaryAlc = IsAssemblyFromSecondaryAlc(GetType().Assembly);

	private partial bool TryGetContentFromSecondaryAlc(out UIElement? content)
	{
		if (IsSecondaryAlcContent(_secondaryAlcContent))
		{
			content = ContentHostOverride?.Content as UIElement;
			return true;
		}

		content = default;
		return false;
	}

	private partial bool TrySetContentFromSecondaryAlc(UIElement? value, ContentControl host, Assembly callingAssembly)
	{
		if (!ShouldRedirectToContentHost(value, callingAssembly))
		{
			return false;
		}

#if DEBUG
		if (callingAssembly.FullName?.StartsWith("System.", StringComparison.Ordinal) == true)
		{
			System.Diagnostics.Debug.WriteLine("Window.Content was set via reflection or framework code; secondary ALC detection may be inaccurate.");
		}
#endif

		UnmarkSecondaryAlcContent(_secondaryAlcContent);
		host.Content = value;
		_secondaryAlcContent = value;
		MarkContentAsSecondaryAlc(value);
		return true;
	}

	/// <summary>
	/// Checks if the given content element is from a secondary AssemblyLoadContext.
	/// When value is null, returns false to allow clearing content.
	/// </summary>
	private bool IsContentFromSecondaryAlc(object? value)
	{
		// Explicitly handle null: a null assignment should clear content, not be detected as secondary ALC
		if (value == null)
		{
			return false;
		}

		return !ReferenceEquals(
			AssemblyLoadContext.Default,
			AssemblyLoadContext.GetLoadContext(value.GetType().Assembly));
	}

	private bool IsSecondaryAlcContent(object? value)
		=> IsContentFromSecondaryAlc(value) || IsContentMarkedAsSecondaryAlc(value);

	private static bool IsContentMarkedAsSecondaryAlc(object? value)
		=> value is not null && _secondaryAlcMarkers.TryGetValue(value, out _);

	private static void MarkContentAsSecondaryAlc(object? value)
	{
		if (value is null)
		{
			return;
		}

		_secondaryAlcMarkers.GetValue(value, static _ => new SecondaryAlcMarker());
	}

	private static void UnmarkSecondaryAlcContent(object? value)
	{
		if (value is null)
		{
			return;
		}

		_secondaryAlcMarkers.Remove(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool ShouldRedirectToContentHost(object? value, Assembly callingAssembly)
	{
		if (IsContentFromSecondaryAlc(value))
		{
			return true;
		}

		if (IsAssemblyFromSecondaryAlc(callingAssembly))
		{
			return true;
		}

		return _isWindowFromSecondaryAlc;
	}

	private static bool IsAssemblyFromSecondaryAlc(Assembly assembly)
	{
		var loadContext = AssemblyLoadContext.GetLoadContext(assembly);
		return loadContext is not null && !ReferenceEquals(loadContext, AssemblyLoadContext.Default);
	}

	/// <summary>
	/// Checks if the content in ContentHostOverride is from a secondary ALC.
	/// </summary>
	private bool IsContentHostedInSecondaryAlc()
	{
		var content = ContentHostOverride?.Content;
		return content is not null
			&& IsSecondaryAlcContent(content);
	}

	private sealed class SecondaryAlcMarker
	{
	}
}
