#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Composition
{
	public partial class CompositionObject
	{
		private class ContextStore
		{
			private readonly object _lock = new object();
			private List<ContextEntry>? _contextEntries;

			public ContextStore()
			{

			}

			public void AddContext(CompositionObject context, string? propertyName)
			{
				lock (_lock)
				{
					AddContextImpl(context, propertyName);
				}
			}

			public void RemoveContext(CompositionObject context, string? propertyName)
			{
				lock (_lock)
				{
					RemoveContextImpl(context, propertyName);
				}
			}

			public void RaiseChanged()
			{
				lock (_lock)
				{
					RaiseChangedImpl();
				}
			}

			private void AddContextImpl(CompositionObject newContext, string? propertyName)
			{
				var contextEntries = _contextEntries;
				if (contextEntries == null)
				{
					contextEntries = new List<ContextEntry>();
					_contextEntries = contextEntries;
				}

				contextEntries.Add(new ContextEntry(newContext, propertyName));
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

			private void RaiseChangedImpl()
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

					context!.OnPropertyChanged(contextEntry.PropertyName, true);
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
}
