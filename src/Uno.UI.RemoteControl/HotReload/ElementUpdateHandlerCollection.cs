using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using static Uno.UI.RemoteControl.HotReload.MetadataUpdater.ElementUpdateAgent;

namespace Uno.UI.HotReload;

internal sealed class ElementUpdateHandlerCollection(ImmutableDictionary<Type, ElementUpdateHandlerActions> handlerActions) : IEnumerable<ElementUpdateHandlerActions>
{
	private readonly Dictionary<Type, ImmutableArray<ElementUpdateHandlerActions>> _cache = new();

	public ImmutableArray<ElementUpdateHandlerActions> Get(Type originalType)
	{
		ref var handlers = ref CollectionsMarshal.GetValueRefOrAddDefault(_cache, originalType, out var hasHandlers);
		if (!hasHandlers)
		{
			handlers = GetCore(originalType);
		}
		return handlers;
	}

	private ImmutableArray<ElementUpdateHandlerActions> GetCore(Type originalType)
	{
		// Get the handler for the type specified.
		// Since we're only interested in handlers for specific element types we exclude those registered for "object".
		// Handlers that want to run for all element types should register for FrameworkElement instead.
		// Handlers are ordered by inheritance depth (most specific first)
		return
		[
			..from handler in handlerActions
			let depth = GetSubClassDepth(originalType, handler.Key)
			where depth is not -1 && handler.Key != typeof(object)
			orderby depth descending
			select handler.Value
		];

		static int GetSubClassDepth(Type? type, Type baseType)
		{
			var count = 0;
			if (type == baseType)
			{
				return 0;
			}
			for (; type != null; type = type.BaseType)
			{
				count++;
				if (type == baseType)
				{
					return count;
				}
			}
			return -1;
		}
	}

	/// <inheritdoc />
	public IEnumerator<ElementUpdateHandlerActions> GetEnumerator()
		=> handlerActions.Values.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
}
