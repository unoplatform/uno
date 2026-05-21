#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Diagnostics;

internal sealed class ElementRefHandleRegistry : IElementRefHandleRegistry
{
	private static readonly Logger _log = typeof(ElementRefHandleRegistry).Log();

	private sealed class RefEntry
	{
		private readonly ElementRefHandleRegistry _registry;

		public RefEntry(int id, ElementRefHandleRegistry registry)
		{
			Id = id;
			_registry = registry;
		}

		public int Id { get; }

		~RefEntry() => _registry.OnObjectCollected(Id);
	}

	private readonly ConditionalWeakTable<DependencyObject, RefEntry> _table = new();
	private readonly ConcurrentDictionary<int, WeakReference<DependencyObject>> _reverse = new();

	// A 32-bit counter supports ~2.1 billion unique handles per session. Diagnostic tools
	// operate on the live visual tree of a running app — exhausting this counter would
	// require registering every frame for ~8 years at 60 fps. No overflow guard needed.
	private int _nextId;

	public string GetOrCreate(DependencyObject element)
	{
		if (!FeatureConfiguration.ElementRefHandle.DisableThreadingCheck && !NativeDispatcher.Main.HasThreadAccess)
		{
			throw new InvalidOperationException("ElementRefHandle should not be accessed from a non-UI thread.");
		}

		ArgumentNullException.ThrowIfNull(element);

		var entry = _table.GetValue(element, e =>
		{
			var id = ++_nextId;
			_reverse[id] = new WeakReference<DependencyObject>(e);
			return new RefEntry(id, this);
		});

		var handle = ToBase36(entry.Id);

		if (_log.IsEnabled(LogLevel.Trace))
		{
			_log.Trace($"ElementRefHandle registered: handle={handle} type={element.GetType().FullName}");
		}

		return handle;
	}

	public bool TryResolve(string? handle, [NotNullWhen(true)] out DependencyObject? element)
	{
		if (!FeatureConfiguration.ElementRefHandle.DisableThreadingCheck && !NativeDispatcher.Main.HasThreadAccess)
		{
			throw new InvalidOperationException("ElementRefHandle should not be accessed from a non-UI thread.");
		}

		element = null;

		if (!TryParseBase36(handle, out var numericId))
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"ElementRefHandle miss: handle={(handle ?? "(null)")} cause=invalid-format");
			}

			return false;
		}

		if (!_reverse.TryGetValue(numericId, out var weakRef))
		{
			if (_log.IsEnabled(LogLevel.Trace))
			{
				_log.Trace($"ElementRefHandle miss: handle={handle} cause=not-registered");
			}

			return false;
		}

		if (weakRef.TryGetTarget(out var target))
		{
			element = target;
			return true;
		}

		// Lazy cleanup: object was GC'd before the RefEntry finalizer ran.
		_reverse.TryRemove(numericId, out _);

		if (_log.IsEnabled(LogLevel.Trace))
		{
			_log.Trace($"ElementRefHandle miss: handle={handle} cause=gc-collected");
		}

		return false;
	}

	private void OnObjectCollected(int id) => _reverse.TryRemove(id, out _);

	private static string ToBase36(int value)
	{
		const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

		if (value <= 0)
		{
			// IDs are issued by ++_nextId starting at 1; a non-positive value is a bug.
			throw new ArgumentOutOfRangeException(nameof(value), value, "Handle ID must be positive.");
		}

		Span<char> buffer = stackalloc char[8];
		int pos = buffer.Length;
		int x = value;

		while (x > 0)
		{
			buffer[--pos] = alphabet[x % 36];
			x /= 36;
		}

		return new string(buffer[pos..]);
	}

	private static bool TryParseBase36(string? s, out int value)
	{
		value = 0;

		if (string.IsNullOrEmpty(s))
		{
			return false;
		}

		// Accumulate into a long to detect int overflow without exception-based control flow.
		long acc = 0;

		foreach (var ch in s)
		{
			int digit = ch switch
			{
				>= '0' and <= '9' => ch - '0',
				>= 'a' and <= 'z' => ch - 'a' + 10,
				>= 'A' and <= 'Z' => ch - 'A' + 10,
				_ => -1
			};

			if (digit < 0)
			{
				return false;
			}

			acc = acc * 36 + digit;

			if (acc > int.MaxValue)
			{
				return false;
			}
		}

		// IDs start at 1; zero is not a valid issued handle.
		if (acc <= 0)
		{
			return false;
		}

		value = (int)acc;
		return true;
	}
}
