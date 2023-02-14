#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Buffers;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml
{
	internal class ResourceBindingCollection
	{
		private readonly Dictionary<DependencyProperty, Dictionary<DependencyPropertyValuePrecedences, ResourceBinding>> _bindings = new Dictionary<DependencyProperty, Dictionary<DependencyPropertyValuePrecedences, ResourceBinding>>();

		public bool HasBindings => _bindings.Count > 0 && _bindings.Any(b => b.Value.Any());

		public IEnumerable<(DependencyProperty Property, ResourceBinding Binding)> GetAllBindings()
		{
			foreach (var kvp in _bindings)
			{
				foreach (var kvpInner in kvp.Value)
				{
					yield return (kvp.Key, kvpInner.Value);
				}
			}
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
			var dict = _bindings.FindOrCreate(property, () => new Dictionary<DependencyPropertyValuePrecedences, ResourceBinding>());
			dict[resourceBinding.Precedence] = resourceBinding;
		}

		public void ClearBinding(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			if (_bindings.TryGetValue(property, out var bindingsByPrecedence))
			{
				bindingsByPrecedence.Remove(precedence);
			}
		}
	}
}
