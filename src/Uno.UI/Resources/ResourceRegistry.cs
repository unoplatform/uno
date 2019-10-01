using Uno.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;

namespace Uno.Presentation.Resources
{
	public class ResourceRegistry : IResourceRegistry
	{
		/// <summary>
		/// Defines a resource selector handler
		/// </summary>
		/// <param name="resourceName">A resource name to be resolved</param>
		/// <returns>A resource instance, otherwise null</returns>
		public delegate object ResourceLookupHandler(string resourceName);

		private object _gate = new object();
		private readonly Dictionary<object, Func<object>> _resources = new Dictionary<object, Func<object>>();
		private List<ResourceLookupHandler> _lookups = new List<ResourceLookupHandler>();

		/// <summary>
		/// Registers a selector that will be used if the no resources are found.
		/// </summary>
		/// <param name="resourceLookup">A selector that returns a resource instance, otherwise null.</param>
		public void RegisteLookup(ResourceLookupHandler resourceLookup)
		{
			_lookups.Add(resourceLookup);
		}

		public void Register(object name, Func<object> builder)
		{
			lock(_gate)
			{
			_resources[name] = builder.AsMemoized();
		}
		}

		/// <summary>
		/// Finds a resource in the registry
		/// </summary>
		/// <param name="name">The name of the resrouce</param>
		/// <returns>The instance of the resource, otherwise null.</returns>
		public object FindResource(string name)
		{
			lock (_gate)
			{
				var resource = _resources.UnoGetValueOrDefault(name, () => null)();

				if (resource == null)
			{
				resource = LookupExternalResouce(name);
			}

			return resource;
		}
		}

		private object LookupExternalResouce(string name)
		{
			foreach (var handler in _lookups)
			{
				var resource = handler(name);

				if (resource != null)
				{
					return resource;
				}
			}

			return null;
		}

		public object GetResource(string name)
		{
			lock (_gate)
			{
			Func<object> value;
			if (_resources.TryGetValue(name, out value))
			{
				return value();
			}
			else
			{
				var resource = LookupExternalResouce(name);

					if (resource != null)
				{
					return resource;
				}

				throw new KeyNotFoundException("Cannot find resource with key '{0}'.".InvariantCultureFormat(name));
				}
			}
		}
	}
}
