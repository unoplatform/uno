// Uno-specific parts of ItemsSourceView.

using System;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Interop;
using Uno.UI.Helpers;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsSourceView
{
	// Uno-specific: referenced by the source, so it must hold the view only weakly.
	private sealed class SourceSubscription : IDisposable
	{
		private readonly WeakReference<ItemsSourceView> _owner;
		private Action _unsubscribe;

		public SourceSubscription(ItemsSourceView owner) => _owner = new WeakReference<ItemsSourceView>(owner);

		public Action Unsubscribe { set => _unsubscribe = value; }

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

		public void Dispose()
		{
			if (global::System.Threading.Interlocked.Exchange(ref _unsubscribe, null) is { } unsubscribe)
			{
				SubscriptionCleanup.Run(unsubscribe, typeof(ItemsSourceView), "an items source subscription");
			}
		}
	}
}
