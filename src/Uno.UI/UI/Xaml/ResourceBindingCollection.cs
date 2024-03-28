#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Buffers;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml;

internal class ResourceBindingCollection
{
	private readonly Dictionary<DependencyProperty, Dictionary<DependencyPropertyValuePrecedences, ResourceBinding>> _bindings = new(DependencyPropertyComparer.Default);
	private BindingEntry[]? _cachedAllBindings;

	public bool HasBindings
	{
		get
		{
			// Avoiding the use of LINQ here to reduce unnecessary enumerator boxing.
			if (_bindings.Count > 0)
			{
				foreach (var entry in _bindings)
				{
					if (entry.Value.Count > 0)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	public record struct BindingEntry(DependencyProperty Property, ResourceBinding Binding);

	public BindingEntry[] GetAllBindings()
	{
		if (_cachedAllBindings is null)
		{
			List<BindingEntry> allBindings = new();

			foreach (var kvp in _bindings)
			{
				foreach (var kvpInner in kvp.Value)
				{
					allBindings.Add(new(kvp.Key, kvpInner.Value));
				}
			}

			// We return a fully materialized list every time
			// as the callers may enumerate the list and new items
			// can be added when resource bindings are evaluated.
			_cachedAllBindings = allBindings.ToArray();
		}

		return _cachedAllBindings;
	}

	public IEnumerable<ResourceBinding>? GetBindingsForProperty(DependencyProperty property)
	{
		if (_bindings.TryGetValue(property, out var bindingsForProperty))
		{
			return bindingsForProperty.Values;
		}

		return null;
	}

	public void Add(DependencyProperty property, ResourceBinding resourceBinding)
	{
		if (!_bindings.TryGetValue(property, out var dict))
		{
			_bindings[property] = dict = new();
		}

		dict[resourceBinding.Precedence] = resourceBinding;

		_cachedAllBindings = null;
	}

	public void ClearBinding(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
	{
		if (_bindings.TryGetValue(property, out var bindingsByPrecedence))
		{
			bindingsByPrecedence.Remove(precedence);

			_cachedAllBindings = null;
		}
	}
}
