#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Windows.UI.Composition
{
	public partial class CompositionObject : IDisposable
	{
		private readonly ContextStore _contextStore = new ContextStore();

		internal CompositionObject()
		{
			ApiInformation.TryRaiseNotImplemented(GetType().FullName!, "The compositor constructor is not available, as the type is not implemented");
			Compositor = new Compositor();
		}

		internal CompositionObject(Compositor compositor)
		{
			Compositor = compositor;
		}

		public Compositor Compositor { get; }

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		public string? Comment { get; set; }

		public void StartAnimation(string propertyName, CompositionAnimation animation)
		{
			StartAnimationCore(propertyName, animation);
		}

		public void StopAnimation(string propertyName)
		{

		}

		internal virtual void StartAnimationCore(string propertyName, CompositionAnimation animation) { }

		internal void AddContext(CompositionObject context, string? propertyName)
		{
			_contextStore.AddContext(context, propertyName);
		}

		internal void RemoveContext(CompositionObject context, string? propertyName)
		{
			_contextStore.RemoveContext(context, propertyName);
		}

		private protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			var fieldCO = field as CompositionObject;
			var valueCO = value as CompositionObject;
			if (fieldCO != null || value != null)
			{
				OnCompositionPropertyChanged(fieldCO, valueCO, propertyName);
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void OnChanged() => OnPropertyChanged(null, false);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue) => OnCompositionPropertyChanged(oldValue, newValue, null);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue, string? propertyName)
		{
			if (oldValue != null)
			{
				oldValue.RemoveContext(this, propertyName);
			}

			if (newValue != null)
			{
				newValue.AddContext(this, propertyName);
			}
		}

		private protected void OnPropertyChanged(string? propertyName, bool isSubPropertyChange)
		{
			OnPropertyChangedCore(propertyName, isSubPropertyChange);
			_contextStore.RaiseChanged();
		}

		private protected virtual void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{

		}

		private class ContextStore
		{
			private readonly object _lock = new object();
			private List<ContextEntry>? _contextEntries = null;

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
