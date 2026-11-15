#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Composition
{
	public partial class CompositionObject
	{
		private readonly object _contextEntriesLock = new object();
		private List<ContextEntry>? _contextEntries;

		// Dead entries (consumers that were collected without ever calling RemoveContext) are swept
		// from AddContext once the list reaches this size. It is then reset to twice the surviving
		// count, so the list stays O(live consumers) at an amortized O(1) cost per AddContext.
		private const int MinContextEntriesPruneThreshold = 16;
		private int _contextEntriesPruneThreshold = MinContextEntriesPruneThreshold;
		private bool _isPropagatingChanged;

		public void AddContext(CompositionObject context, string? propertyName)
		{
			lock (_contextEntriesLock)
			{
				AddContextImpl(context, propertyName);
			}
		}

		public void RemoveContext(CompositionObject context, string? propertyName)
		{
			lock (_contextEntriesLock)
			{
				RemoveContextImpl(context, propertyName);
			}
		}

		public void PropagateChanged()
		{
			lock (_contextEntriesLock)
			{
				PropagateChangedImpl();
			}
		}

		private void AddContextImpl(CompositionObject newContext, string? propertyName)
		{
			var contextEntries = _contextEntries;
			if (contextEntries == null)
			{
				contextEntries = new List<ContextEntry>();
				_contextEntries = contextEntries;
				_contextEntriesPruneThreshold = MinContextEntriesPruneThreshold;
			}
			else if (contextEntries.Count >= _contextEntriesPruneThreshold && !_isPropagatingChanged)
			{
				// A consumer (e.g. a BorderVisual or CompositionSpriteShape using a shared theme brush) only
				// removes its entry when its property is reassigned; when it is simply collected, its entry
				// stays here. Shared brushes rarely raise changes (the other place that prunes), so without
				// this sweep the list, and the GC handle of each WeakReference, grows without bound.
				// Skipped while PropagateChangedImpl is iterating the list by index.
				contextEntries.RemoveAll(static entry => !entry.Context.TryGetTarget(out _));
				_contextEntriesPruneThreshold = Math.Max(MinContextEntriesPruneThreshold, contextEntries.Count * 2);
			}

			contextEntries.Add(new ContextEntry(newContext, propertyName));
		}

		/// <summary>
		/// Test hook: the number of context entries (alive or dead) currently tracked by this object.
		/// </summary>
		internal int ContextEntriesCount
		{
			get
			{
				lock (_contextEntriesLock)
				{
					return _contextEntries?.Count ?? 0;
				}
			}
		}

		private void RemoveContextImpl(CompositionObject oldContext, string? propertyName)
		{
			var contextEntries = _contextEntries;
			if (contextEntries == null)
			{
				return;
			}

			for (int i = contextEntries.Count - 1; i >= 0; i--)
			{
				var contextEntry = contextEntries[i];

				if (!contextEntry.Context.TryGetTarget(out CompositionObject? context))
				{
					// Clean up dead references.
					contextEntries.RemoveAt(i);
					continue;
				}

				if (context == oldContext && contextEntry.PropertyName == propertyName)
				{
					contextEntries.RemoveAt(i);
					break;
				}
			}

			if (contextEntries.Count == 0)
			{
				_contextEntries = null;
			}
		}

		private void PropagateChangedImpl()
		{
			var contextEntries = _contextEntries;
			if (contextEntries == null)
			{
				return;
			}

			var wasPropagatingChanged = _isPropagatingChanged;
			_isPropagatingChanged = true;
			try
			{
				for (int i = contextEntries.Count - 1; i >= 0; i--)
				{
					var contextEntry = contextEntries[i];

					if (!contextEntry.Context.TryGetTarget(out var context))
					{
						// Clean up dead references.
						contextEntries.RemoveAt(i);
						continue;
					}

					context.OnPropertyChanged(contextEntry.PropertyName, true);
				}
			}
			finally
			{
				_isPropagatingChanged = wasPropagatingChanged;
			}
		}

		private class ContextEntry
		{
			public ContextEntry(CompositionObject context, string? propertyName)
			{
				Context = new WeakReference<CompositionObject>(context);
				PropertyName = propertyName;
			}

			public WeakReference<CompositionObject> Context { get; }
			public string? PropertyName { get; }
		}
	}
}
