using Uno.Extensions;
using Uno.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.Diagnostics.Eventing;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Devices.Enumeration;
using Uno.UI.Xaml.Media;


#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Microsoft.UI.Xaml.Media
{
	partial class ImageSource
	{
		private readonly SerialDisposable _opening = new SerialDisposable();

		// Every subscription's callback, held weakly, as a source that outlives its consumers (e.g. a shared icon) must not keep
		// them alive (see SubscribeWeakly).
		private readonly List<WeakReference<Action<ImageData>>> _subscriptions = new List<WeakReference<Action<ImageData>>>();

		// The subscriptions made with Subscribe, which the source keeps alive until they are disposed.
		private readonly List<Subscription> _heldSubscriptions = new List<Subscription>();

		protected ImageSource()
		{

		}

		partial void InitFromResource(Uri uri)
		{
			// TODO: Unify
#if __SKIA__
			AbsoluteUri = uri;
#else
			var path = uri.PathAndQuery.TrimStart("/");
			AbsoluteUri = new Uri(path, UriKind.Relative);
#endif
		}

		partial void CleanupResource()
		{
			AbsoluteUri = null;
		}

		/// <summary>
		/// Subscribes to this image source. The source keeps the subscriber alive until the returned disposable is disposed.
		/// </summary>
		/// <param name="onSourceOpened">
		/// A callback that will be invoked each time the source has been updated
		/// (i.e. a property has changed on the source AND the source has been re-opened)
		/// </param>
		/// <returns>A disposable that unsubscribes the callback.</returns>
		internal IDisposable Subscribe(Action<ImageData> onSourceOpened) => Subscribe(onSourceOpened, weakly: false);

		/// <summary>
		/// Subscribes to this image source without keeping the subscriber alive, for a subscriber the source may outlive
		/// (an Image or ImageBrush showing a shared icon).
		/// </summary>
		/// <returns>
		/// A disposable that unsubscribes the callback. It is also what keeps the callback alive, so the subscriber must
		/// hold it for as long as it wants to be notified.
		/// </returns>
		internal IDisposable SubscribeWeakly(Action<ImageData> onSourceOpened) => Subscribe(onSourceOpened, weakly: true);

		private IDisposable Subscribe(Action<ImageData> onSourceOpened, bool weakly)
		{
			var subscription = new Subscription(this, onSourceOpened);
			_subscriptions.Add(subscription.Callback);
			if (!weakly)
			{
				_heldSubscriptions.Add(subscription);
			}

			if (_imageData.HasData)
			{
				onSourceOpened(_imageData);
			}
			else if (PruneSubscriptions() == 1)
			{
				Open();
			}

			return subscription;
		}

		/// <summary>
		/// Indicates that this source has already been opened
		/// (So the onSourceOpened callback of Subscribe will be invoked synchronously!)
		/// </summary>
		internal bool IsOpened => _imageData.HasData;

		private protected void InvalidateSource()
		{
			_imageData = default;
			if (PruneSubscriptions() > 0 || this is SvgImageSource)
			{
				Open();
			}
		}

		private void Open()
		{
			var cts = new CancellationTokenSource();
			var ct = cts.Token;
			_opening.Disposable = Disposable.Create(cts.Cancel);
			try
			{
				if (TryOpenSourceSync(null, null, out var img))
				{
					OnOpened(img);
				}
				else if (TryOpenSourceAsync(ct, null, null, out var asyncImg))
				{
					// Keep the subscribers alive until the source has opened: one waiting for it (e.g. an app awaiting
					// a brush's ImageOpened) may have nothing else referencing it.
					var subscribers = GetSubscribers();

					DispatcherQueue.TryEnqueue(async () =>
					{
						try
						{
							var data = await asyncImg;
							if (!ct.IsCancellationRequested)
							{
								OnOpened(data);
							}
						}
						catch (OperationCanceledException) when (ct.IsCancellationRequested)
						{
						}
						catch (Exception error)
						{
							OnOpened(ImageData.FromError(error));
						}

						GC.KeepAlive(subscribers);
					});
				}
				else
				{
					OnOpened(new ImageData()); // Empty
				}
			}
			catch (Exception error)
			{
				OnOpened(ImageData.FromError(error));
			}
		}

		private void OnOpened(ImageData data)
		{
			_imageData = data; // We should also cache the targetWidth and targetHeight

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Image {this} opened with {data}");
			}

			foreach (var subscriber in GetSubscribers())
			{
				subscriber(data);
			}
		}

		/// <summary>
		/// Removes the subscriptions whose subscriber has been collected, and returns how many remain.
		/// </summary>
		private int PruneSubscriptions()
		{
			_subscriptions.RemoveAll(static subscription => !subscription.TryGetTarget(out _));
			return _subscriptions.Count;
		}

		/// <summary>
		/// Removes the subscriptions whose subscriber has been collected, and returns the callbacks of the others.
		/// </summary>
		private List<Action<ImageData>> GetSubscribers()
		{
			var subscribers = new List<Action<ImageData>>(_subscriptions.Count);
			_subscriptions.RemoveAll(subscription =>
			{
				if (subscription.TryGetTarget(out var subscriber))
				{
					subscribers.Add(subscriber);
					return false;
				}

				return true;
			});

			return subscribers;
		}

		private sealed class Subscription : IDisposable
		{
			private readonly ImageSource _owner;
			private Action<ImageData> _onSourceOpened; // Keeps the callback alive while this is held.

			public Subscription(ImageSource owner, Action<ImageData> onSourceOpened)
			{
				_owner = owner;
				_onSourceOpened = onSourceOpened;
				Callback = new WeakReference<Action<ImageData>>(onSourceOpened);
			}

			public WeakReference<Action<ImageData>> Callback { get; }

			public void Dispose()
			{
				if (_onSourceOpened is not null)
				{
					_onSourceOpened = null;
					_owner._subscriptions.Remove(Callback);
					_owner._heldSubscriptions.Remove(this);
				}
			}
		}
	}
}
