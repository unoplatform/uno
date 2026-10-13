#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Per-object storage of <see cref="ResourceBinding"/>s, keyed by (property, precedence).
/// </summary>
/// <remarks>
/// A flat list scanned linearly, like <see cref="ThemeResourceMap"/>: even a ListViewItemPresenter carries only ~40
/// entries, few enough for a scan to cost less than a dictionary per property.
/// </remarks>
internal class ResourceBindingCollection
{
	private readonly List<BindingEntry> _bindings = new();
	private BindingEntry[]? _cachedAllBindings;

	public bool HasBindings => _bindings.Count > 0;

	public record struct BindingEntry(DependencyProperty Property, ResourceBinding Binding);

	public BindingEntry[] GetAllBindings()
		// We return a fully materialized snapshot as the callers may enumerate it
		// and new items can be added when resource bindings are evaluated.
		=> _cachedAllBindings ??= _bindings.ToArray();

	public IEnumerable<ResourceBinding>? GetBindingsForProperty(DependencyProperty property)
	{
		List<ResourceBinding>? result = null;

		foreach (var entry in _bindings)
		{
			if (entry.Property == property)
			{
				(result ??= new()).Add(entry.Binding);
			}
		}

		return result;
	}

	public void Add(DependencyProperty property, ResourceBinding resourceBinding)
	{
		_cachedAllBindings = null;

		var index = IndexOf(property, resourceBinding.Precedence);
		if (index >= 0)
		{
			_bindings[index] = new(property, resourceBinding);
		}
		else
		{
			_bindings.Add(new(property, resourceBinding));
		}
	}

	public void ClearBinding(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
	{
		var index = IndexOf(property, precedence);
		if (index >= 0)
		{
			_bindings.RemoveAt(index);
			_cachedAllBindings = null;
		}
	}

	private int IndexOf(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
	{
		for (var i = 0; i < _bindings.Count; i++)
		{
			var entry = _bindings[i];
			if (entry.Property == property && entry.Binding.Precedence == precedence)
			{
				return i;
			}
		}

		return -1;
	}
}
