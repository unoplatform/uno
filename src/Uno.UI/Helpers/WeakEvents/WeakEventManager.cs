#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Uno.UI.Helpers;

internal sealed class WeakEventManager
{
	private readonly ConditionalWeakTable<object, Dictionary<string, List<Action>>> _handlersPerTarget = new();
	private Dictionary<string, List<Action>>? _staticHandlers;
	private string? _enumeratingHandlersOfEvent;
	private Stack<string>? _enumeratingHandlersOfEventStack;

	public void AddEventHandler(Action? handler, [CallerMemberName] string eventName = "")
	{
		ArgumentException.ThrowIfNullOrEmpty(eventName);

		if (handler is null)
		{
			// Allow handler to be nullable to not have to deal with nullability checks in callsites, but this indicates a bug.
			Debug.Fail($"Got a null event handler for event {eventName}.");
			return;
		}

		var target = handler.Target;
		if (target is null)
		{
			AddTo(_staticHandlers ??= new(), eventName, handler);
		}
		else
		{
			AddTo(_handlersPerTarget.GetValue(target, static _ => new()), eventName, handler);
		}
	}

	public void RemoveEventHandler(Action? handler, [CallerMemberName] string eventName = "")
	{
		ArgumentException.ThrowIfNullOrEmpty(eventName);

		if (handler is null)
		{
			// Allow handler to be nullable to not have to deal with nullability checks in callsites, but this indicates a bug.
			Debug.Fail($"Got a null event handler for event {eventName}.");
			return;
		}

		var target = handler.Target;
		if (target is null)
		{
			if (_staticHandlers is not null)
			{
				RemoveFrom(_staticHandlers, eventName, handler);
			}
		}
		else if (_handlersPerTarget.TryGetValue(target, out var handlersPerEvent))
		{
			RemoveFrom(handlersPerEvent, eventName, handler);
		}
	}

	public void HandleEvent(string eventName)
	{
		if (_enumeratingHandlersOfEvent is not null)
		{
			_enumeratingHandlersOfEventStack ??= new();
			_enumeratingHandlersOfEventStack.Push(_enumeratingHandlersOfEvent);
		}
		_enumeratingHandlersOfEvent = eventName;

		try
		{
			foreach (var kvp in _handlersPerTarget)
			{
				if (!kvp.Value.TryGetValue(eventName, out var handlers))
				{
					// Event handlers not found for this target
					continue;
				}

				var count = handlers.Count;
				for (var i = 0; i < count; i++)
				{
					handlers[i]();
				}
			}
		}
		finally
		{
			_enumeratingHandlersOfEvent = (_enumeratingHandlersOfEventStack?.TryPop(out var previous) ?? false) ? previous : null;
		}
	}

	private void AddTo(Dictionary<string, List<Action>> handlersPerEvent, string eventName, Action handler)
	{
		ref var handlers = ref CollectionsMarshal.GetValueRefOrAddDefault(handlersPerEvent, eventName, out var exists);
		if (exists)
		{
			if (_enumeratingHandlersOfEvent == eventName)
			{
#if NET8_0_OR_GREATER
				handlers = handlers![..];
#else
				handlers = handlers!.ToArray();
#endif
			}

			handlers!.Add(handler);
		}
		else
		{
			handlers = [handler];
		}
	}

	private void RemoveFrom(Dictionary<string, List<Action>> handlersPerEvent, string eventName, Action handler)
	{
		ref var handlers = ref CollectionsMarshal.GetValueRefOrNullRef(handlersPerEvent, eventName);
		if (!Unsafe.IsNullRef(ref handlers))
		{
			if (_enumeratingHandlersOfEvent == eventName)
			{
#if NET8_0_OR_GREATER
				handlers = handlers[..];
#else
				handlers = handlers!.ToArray();
#endif
			}

			handlers.Remove(handler);
		}
	}
}
