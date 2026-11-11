// Uno-specific parts of ItemsSourceView.

using System;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Interop;
using Uno.UI.Helpers;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsSourceView
{
	// Like WinUI's raw-'this' delegates, the source must not keep this view alive: it references a forwarder holding this
	// view weakly. Only this view references the returned subscription, whose finalizer removes the forwarder from the
	// source once this view is collected, unless the source died too.
	private static IDisposable Track<TSource>(TSource source, SourceForwarder forwarder, Action<TSource, SourceForwarder> unsubscribe)
		where TSource : class
		=> new WeakSourceSubscription<TSource, SourceForwarder>(
			source,
			forwarder,
			unsubscribe,
			onDisposed: null,
			typeof(ItemsSourceView),
			"an items source subscription");

	// Referenced by the source, so it must hold the view only weakly.
	private sealed class SourceForwarder
	{
		private readonly WeakReference<ItemsSourceView> _owner;

		public SourceForwarder(ItemsSourceView owner) => _owner = new WeakReference<ItemsSourceView>(owner);

		// Not unsubscribed here once the view is dead: a source may raise over its live handler list.
		public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				owner.OnCollectionChanged(sender, e);
			}
		}

		public void OnBindableVectorChanged(IBindableObservableVector sender, object e)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				owner.OnBindableVectorChanged(sender, e);
			}
		}

		public void OnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				owner.OnVectorChanged(sender, e);
			}
		}

		public void OnUntypedVectorChanged(object sender, IVectorChangedEventArgs e)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				owner.OnUntypedVectorChanged(sender, e);
			}
		}
	}
}
