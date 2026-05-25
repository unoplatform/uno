#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Per-DependencyObject storage mapping (DependencyProperty, Precedence) to <see cref="ThemeResourceReference"/>.
/// </summary>
/// <remarks>
/// MUX Reference: ThemeResourceMap in CDOAssociative.h
/// In WinUI this is a vector_map&lt;KnownPropertyIndex, CThemeResource*&gt;.
/// We additionally key by precedence since Uno's DP system supports multiple precedence levels.
/// </remarks>
internal sealed class ThemeResourceMap
{
	// Use a list for small maps (most elements have &lt; 10 theme resource bindings).
	// The (property, precedence) composite key ensures uniqueness.
	private readonly List<Entry> _entries = new();

	public bool HasEntries => _entries.Count > 0;

	/// <summary>
	/// Stores or replaces a theme resource reference for the given property and precedence.
	/// </summary>
	public void Set(DependencyProperty property, DependencyPropertyValuePrecedences precedence, ThemeResourceReference reference)
	{
		for (var i = 0; i < _entries.Count; i++)
		{
			if (_entries[i].Property == property && _entries[i].Precedence == precedence)
			{
				_entries[i] = new Entry(property, precedence, reference);
				return;
			}
		}

		_entries.Add(new Entry(property, precedence, reference));
	}

	/// <summary>
	/// Removes the theme resource reference for the given property and precedence.
	/// </summary>
	public void Clear(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
	{
		for (var i = 0; i < _entries.Count; i++)
		{
			if (_entries[i].Property == property && _entries[i].Precedence == precedence)
			{
				_entries.RemoveAt(i);
				return;
			}
		}
	}

	/// <summary>
	/// Removes all theme resource references for the given property (all precedences).
	/// </summary>
	public void ClearAll(DependencyProperty property)
	{
		for (var i = _entries.Count - 1; i >= 0; i--)
		{
			if (_entries[i].Property == property)
			{
				_entries.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Gets the theme resource reference for the given property and precedence, if any.
	/// </summary>
	public ThemeResourceReference? Get(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
	{
		for (var i = 0; i < _entries.Count; i++)
		{
			if (_entries[i].Property == property && _entries[i].Precedence == precedence)
			{
				return _entries[i].Reference;
			}
		}

		return null;
	}

	/// <summary>
	/// Returns all entries for iteration during theme walk.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::UpdateAllThemeReferences() in Theming.cpp
	/// WinUI iterates ThemeResourceMap and calls UpdateThemeReference for each entry.
	/// </remarks>
	public List<Entry> GetAll() => _entries;

	/// <summary>
	/// Gets all references for the given property (across all precedences).
	/// </summary>
	public IEnumerable<ThemeResourceReference> GetForProperty(DependencyProperty property)
	{
		for (var i = 0; i < _entries.Count; i++)
		{
			if (_entries[i].Property == property)
			{
				yield return _entries[i].Reference;
			}
		}
	}

	internal struct Entry
	{
		public DependencyProperty Property;
		public DependencyPropertyValuePrecedences Precedence;
		public ThemeResourceReference Reference;

		public Entry(DependencyProperty property, DependencyPropertyValuePrecedences precedence, ThemeResourceReference reference)
		{
			Property = property;
			Precedence = precedence;
			Reference = reference;
		}
	}
}
