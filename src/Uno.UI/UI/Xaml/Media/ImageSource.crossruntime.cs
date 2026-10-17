using Uno.Extensions;
using Uno.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.UI.Composition;
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
		// In subscription order, which is the notification order; a linked list, so unsubscribing is O(1).
		private readonly LinkedList<Subscription> _subscriptions = new();

		// Whether the async open in _opening has yet to deliver: a subscriber arriving meanwhile waits for it.
		private bool _isOpening;

		// What the source keeps of each surface its subscriptions draw or were given, or that it replaced while they did.
		private Dictionary<ICompositionSurface, Held> _held;

		private sealed class Held(ImageData data)
		{
			internal readonly ImageData Data = data;

			// How many times the subscriptions' Shown and Pending refer to it.
			internal int Count;

			// How many Images drew it when they left: once nothing holds it, it waits the full delay, for them to come back.
			internal int LeftWith;

			// Replaced, releasable and held by none: its wait to be released.
			internal PendingImageReleases.Entry Wait;

			// For data the cache shares, this source's use of it while a subscription holds it (SharedImageDecode).
			internal SharedImageDecode.SourceUse Use;
		}

		// Changes whenever _imageData is replaced.
		private int _dataVersion;

		// What the shared decodes this source uses know it by.
		private SharedImageDecode.User _shareUser;

		// The wait of what this source decoded while nothing subscribes to it.
		private PendingImageReleases.Entry _pendingRelease;

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
		/// Subscribes to this image source
		/// </summary>
		/// <param name="onSourceOpened">
		/// A callback that will be invoked each time the source has been updated
		/// (i.e. a property has changed on the source AND the source has been re-opened)
		/// </param>
		/// <param name="shown">
		/// What the subscriber still draws from an earlier subscription to this source (an Image back in the tree). While the
		/// source has data or is opening, that is kept until the subscriber draws what replaced it; once the source failed
		/// or was cleared, the subscriber is given an empty image instead, so it stops drawing it.
		/// </param>
		/// <param name="appliedOnDelivery">
		/// Whether the subscriber draws what it is given at once (a brush). One that draws it later (an Image, when measured)
		/// calls <see cref="Subscription.Applied"/> then, and what it drew until then stays meanwhile.
		/// </param>
		internal Subscription Subscribe(Action<ImageData> onSourceOpened, ICompositionSurface shown = null, bool appliedOnDelivery = true)
		{
			// What a subscriber still draws stays held until it draws the replacement, which only one that draws later does.
			Debug.Assert(shown is null || !appliedOnDelivery, "Only a subscriber that draws later passes what it still draws.");

			var subscription = new Subscription(this, onSourceOpened, appliedOnDelivery, KeepReplaced(shown));
			subscription.Node = _subscriptions.AddLast(subscription);
			CancelPendingRelease();

			if (HasLiveData)
			{
				subscription.Deliver(_imageData);
			}
			else if (_subscriptions.Count == 1 && !_isOpening && !IsFailed)
			{
				Open();
			}
			else if (shown is not null && !_isOpening)
			{
				// The source failed or was cleared while the subscriber was away: it must stop drawing what it had, which the
				// cache may keep alive. It gets an empty image, not the error: as on WinUI, attaching to a source that already
				// failed raises nothing.
				subscription.Deliver(ImageData.Empty);
			}

			return subscription;
		}

		/// <summary>
		/// A subscriber to this source, and what it draws and was given, which the source does not release.
		/// </summary>
		internal sealed class Subscription : IDisposable
		{
			private readonly ImageSource _source;
			private readonly Action<ImageData> _callback;
			private bool _isDisposed;

			internal Subscription(ImageSource source, Action<ImageData> callback, bool appliedOnDelivery, ImageData shown)
			{
				_source = source;
				_callback = callback;
				AppliedOnDelivery = appliedOnDelivery;
				Set(shown, default);
			}

			/// <summary>
			/// Whether the subscriber draws what it is given at once (see <see cref="Subscribe"/>).
			/// </summary>
			internal bool AppliedOnDelivery { get; }

			/// <summary>
			/// What the subscriber draws: what it last applied, or still draws from an earlier subscription.
			/// </summary>
			internal ImageData Shown { get; private set; }

			/// <summary>
			/// What the subscriber was given and has yet to draw.
			/// </summary>
			internal ImageData Pending { get; private set; }

			/// <summary>
			/// Whether the subscription was not disposed, which also takes it out of the source's subscriptions.
			/// </summary>
			internal bool IsActive => !_isDisposed;

			internal LinkedListNode<Subscription> Node { get; set; }

			// Keeps the source's hold counts in step with Shown and Pending.
			private void Set(ImageData shown, ImageData pending)
			{
				if (!_isDisposed)
				{
					_source.CountHold(shown, 1);
					_source.CountHold(pending, 1);
					_source.CountHold(Shown, -1);
					_source.CountHold(Pending, -1);
				}

				Shown = shown;
				Pending = pending;
			}

			internal void Deliver(ImageData data)
			{
				// Before the callback, which may change the source and so be given newer data, or throw.
				if (AppliedOnDelivery)
				{
					Set(data, default);
				}
				else
				{
					Set(Shown, data);
				}

				_callback(data);
			}

			/// <summary>
			/// Tells the source that the subscriber now draws <paramref name="data"/>, the data it was last given (or an empty
			/// image once it draws nothing), so what it drew before can be released.
			/// </summary>
			internal void Applied(ImageData data)
			{
				if (!_isDisposed)
				{
					Set(data, default);
					_source.OnApplied();
				}
			}

			public void Dispose()
			{
				if (!_isDisposed)
				{
					_isDisposed = true;
					_source.Unsubscribe(this);
				}
			}
		}

		private void Unsubscribe(Subscription subscription)
		{
			_subscriptions.Remove(subscription.Node);

			// An Image keeps drawing what it drew off the tree (its sprite), and may come back with it.
			if (!subscription.AppliedOnDelivery && IsReleasable(subscription.Shown))
			{
				GetHeld(subscription.Shown).LeftWith++;
			}

			CountHold(subscription.Shown, -1);
			CountHold(subscription.Pending, -1);
			ReleaseUnheld(afterPass: false);

			if (_subscriptions.Count == 0)
			{
				ScheduleReleaseUnused();
			}
		}

		private void OnApplied() => ReleaseUnheld(afterPass: true);

		private Held GetHeld(ImageData data)
		{
			var held = _held ??= new(ReferenceEqualityComparer.Instance);
			if (!held.TryGetValue(data.CompositionSurface, out var record))
			{
				held[data.CompositionSurface] = record = new Held(data);
			}

			return record;
		}

		private void CountHold(ImageData data, int change)
		{
			if (data.CompositionSurface is not { } surface)
			{
				return;
			}

			var held = GetHeld(data);
			held.Count += change;
			Debug.Assert(held.Count >= 0, "A subscription let go of a surface it did not hold.");

			// A shared decode counts this source as one of its users while any of its subscriptions holds it.
			if (held.Data.Share is { } share)
			{
				if (change > 0 && held.Count == 1 && !share.IsReleased)
				{
					held.Use = share.AddSource(_shareUser ??= new());
				}
				else if (held.Count == 0 && held.Use is { } use)
				{
					held.Use = null;
					share.RemoveSource(use, _shareUser);
				}
			}

			if (change > 0 && held.Wait is { } wait)
			{
				// Held again: it no longer waits.
				PendingImageReleases.Remove(wait);
				held.Wait = null;
			}
			else if (held.Count == 0 && held.LeftWith == 0 && (ReferenceEquals(surface, _imageData.CompositionSurface) || !IsReleasable(held.Data)))
			{
				// The current data, or data the cache shares: nothing to wait for.
				_held.Remove(surface);
			}
		}

		/// <summary>
		/// Changes when what this source shows changes (a new UriSource or stream, an invalidation), but not when its
		/// decoded data is only released and decoded again: the opened events are raised once per version, as WinUI
		/// raises them once per source.
		/// </summary>
		internal int SourceVersion { get; private protected set; }

		/// <summary>
		/// Releases what this source decoded for its subscribers once it has waited for one to come back (see
		/// <see cref="PendingImageReleases"/>), so an element that leaves the tree and enters it again meanwhile
		/// (re-parenting, re-templating, container recycling) keeps its pixels.
		/// </summary>
		private void ScheduleReleaseUnused()
		{
			// An SvgImageSource opens without subscribers and keeps its parsed vector document until its source changes.
			// Data shared through the bitmap cache stays: the source never releases it.
			if (this is not SvgImageSource
				&& _pendingRelease is null
				&& (IsReleasable(_imageData) || _isOpening))
			{
				_pendingRelease = PendingImageReleases.Add(GetByteCount(_imageData), ReleaseUnused);
			}
		}

		private void CancelPendingRelease()
		{
			if (_pendingRelease is { } pending)
			{
				PendingImageReleases.Remove(pending);
				_pendingRelease = null;
			}
		}

		private static long GetByteCount(ImageData data)
			=> IsReleasable(data) ? ((CompositionImageSurface)data.CompositionSurface!).DecodedByteCount : 0;

		private void ReleaseUnused()
		{
			_pendingRelease = null;

			// An open nothing waits for is stopped, which cancels an uncached download; data shared through the cache stays.
			if (_isOpening || IsReleasable(_imageData))
			{
				ReleaseImageData();
			}
		}

		/// <summary>
		/// Cancels an open in flight and drops the decoded data, for a change of the source. Data this source owns is
		/// released at once when nothing shows it, and otherwise once the subscribers have what replaces it.
		/// </summary>
		private void ReleaseImageData()
		{
			_opening.Disposable = null;
			_isOpening = false;

			if (_subscriptions.Count == 0)
			{
				CancelPendingRelease();
				ReleaseReplaced();
				ReleaseSurface(_imageData);
			}
			else if (IsReleasable(_imageData))
			{
				// The subscribers keep showing it until they have what replaces it, as WinUI keeps the surface while it
				// switches URIs, to avoid flickering (MUX Reference: CImageSource::ReloadImage, imagesource.cpp). Its wait
				// starts once nothing holds it, never here: a WriteableBitmap opens into the same surface right after.
				GetHeld(_imageData);
			}

			_imageData = ImageData.Empty;
			_dataVersion++;
		}

		// Nothing subscribes, so nothing holds what the source replaced.
		private void ReleaseReplaced()
		{
			if (_held is { } held)
			{
				_held = null;
				foreach (var (surface, record) in held)
				{
					if (record.Wait is { } wait)
					{
						PendingImageReleases.Remove(wait);
					}

					if (!ReferenceEquals(surface, _imageData.CompositionSurface))
					{
						ReleaseSurface(record.Data);
					}
				}
			}
		}

		/// <summary>
		/// Starts the wait of the replaced data nothing holds anymore: the full delay, or after the pass for what a subscriber
		/// stopped drawing because it drew the replacement, unless an Image left with it (see <see cref="KeepReplaced"/>).
		/// </summary>
		private void ReleaseUnheld(bool afterPass)
		{
			if (_held is null)
			{
				return;
			}

			foreach (var (surface, held) in _held)
			{
				// A WriteableBitmap draws its new pixels into the current surface, which never waits.
				if (held.Count > 0
					|| held.Wait is not null
					|| ReferenceEquals(surface, _imageData.CompositionSurface)
					|| !IsReleasable(held.Data))
				{
					continue;
				}

				var isAfterPass = afterPass && held.LeftWith == 0;
				var data = held.Data;

				// What goes after the pass is released then anyway: counting it would have older waiting images go too.
				held.Wait = PendingImageReleases.Add(
					isAfterPass ? 0 : GetByteCount(data),
					() =>
					{
						_held?.Remove(surface);
						ReleaseSurface(data);
					},
					isAfterPass);
			}
		}

		/// <summary>
		/// What a subscriber back with <paramref name="surface"/> draws: the current data, or replaced data, taken back from its
		/// wait, as long as the subscriber will be given what replaced it.
		/// </summary>
		private ImageData KeepReplaced(ICompositionSurface surface)
		{
			// A source that failed or was cleared gives nothing more: the subscriber must show nothing, not keep the old image.
			// Without subscribers, Subscribe opens the source, unless it failed.
			if (surface is null || !(_isOpening || _imageData.HasData || (_subscriptions.Count == 0 && !IsFailed)))
			{
				return default;
			}

			if (_held?.TryGetValue(surface, out var held) == true)
			{
				// The subscription's hold, counted once it is created, ends the wait; other Images may still be away with it.
				if (held.LeftWith > 0)
				{
					held.LeftWith--;
				}

				return held.Data;
			}

			// A shared decode no subscription holds waits on its own: the subscriber becomes one of its users again.
			return ReferenceEquals(surface, _imageData.CompositionSurface) ? _imageData : SharedImageDecode.FindLive(surface);
		}

		// A failure stays until the source changes, as on WinUI, where a source that needs no reload is not reloaded for an
		// element that attaches to it (MUX Reference: CImageSource::ReloadImage, imagesource.cpp): it is not retried.
		private bool IsFailed => _imageData.Kind == ImageDataKind.Error;

		private static bool IsReleasable(ImageData data) => !data.IsShared && data.CompositionSurface is CompositionImageSurface;

		/// <summary>
		/// Releases the decoded frames of an image surface that nothing will display, unless the data is shared through
		/// the bitmap cache, in which case it is the cache's to release.
		/// </summary>
		private protected static void ReleaseSurface(ImageData data)
		{
			if (IsReleasable(data))
			{
				((CompositionImageSurface)data.CompositionSurface!).ReleaseFrames();
			}
		}

		/// <summary>
		/// Indicates that this source has already been opened
		/// (So the onSourceOpened callback of Subscribe will be invoked synchronously!)
		/// </summary>
		internal bool IsOpened => HasLiveData;

		// A source keeps shared data while nothing subscribes to it, but the shared decode may be released meanwhile: the
		// source then has no data, and its next subscriber opens it again.
		private bool HasLiveData => _imageData.HasData && _imageData.Share is not { IsReleased: true };

		private protected void InvalidateSource()
		{
			ReleaseImageData();
			SourceVersion++;

			if (_subscriptions.Count > 0 || this is SvgImageSource)
			{
				Open();
			}
		}

		private void Open()
		{
			var cts = new CancellationTokenSource();
			var ct = cts.Token;
			_opening.Disposable = Disposable.Create(cts.Cancel);

			// In progress until Complete, also while the loaders run app code (a result already at hand is published before
			// TryOpenSourceAsync returns): a subscriber that leaves or arrives meanwhile sees an open in flight.
			_isOpening = true;
			try
			{
				if (TryOpenSourceSync(null, null, out var img))
				{
					Complete(img);
				}
				else if (TryOpenSourceAsync(ct, null, null, out var asyncImg))
				{
					var isEnqueued = DispatcherQueue.TryEnqueue(async () =>
					{
						try
						{
							Complete(await asyncImg);
						}
						catch (OperationCanceledException) when (ct.IsCancellationRequested)
						{
						}
						catch (Exception error)
						{
							Complete(ImageData.FromError(error));
						}
					});

					if (!ct.IsCancellationRequested)
					{
						_isOpening = isEnqueued;
					}
				}
				else
				{
					Complete(ImageData.Empty);
				}
			}
			catch (Exception error)
			{
				Complete(ImageData.FromError(error));
			}

			// Opening runs app code (a completed cache entry raises ImageOpened before TryOpenSourceAsync returns) that may
			// supersede the open or dispose the source: only an open still current updates the source. A superseded one
			// releases what it produced, unless it is the one surface a WriteableBitmap opens into.
			void Complete(ImageData data)
			{
				try
				{
					if (!ct.IsCancellationRequested)
					{
						_isOpening = false;
						OnOpened(data);
					}
					else if (!ReferenceEquals(data.CompositionSurface, _imageData.CompositionSurface))
					{
						ReleaseSurface(data);
					}
				}
				finally
				{
					// A shared result comes with its open as one of its users, which ends once the subscriptions it was delivered
					// to hold it, even if one of them threw.
					data.Share?.EndOpen();
				}
			}
		}

		private void OnOpened(ImageData data)
		{
			_imageData = data; // We should also cache the targetWidth and targetHeight
			var version = ++_dataVersion;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Image {this} opened with {data}");
			}

			// A listener can unsubscribe others, or change the source and so release this data: only listeners still
			// subscribed get it, and only while it is still the source's. The ones a change skipped keep what they had.
			foreach (var subscription in _subscriptions.ToArray())
			{
				if (_dataVersion != version)
				{
					break;
				}

				if (subscription.IsActive)
				{
					subscription.Deliver(data);
				}
			}

			ReleaseUnheld(afterPass: true);

			// A decode that completed while nothing subscribes waits in place of the open, or like any undisplayed image.
			if (_pendingRelease is { } pending)
			{
				PendingImageReleases.Resize(pending, GetByteCount(_imageData));
			}
			else if (_subscriptions.Count == 0)
			{
				ScheduleReleaseUnused();
			}
		}

		/// <summary>
		/// Opens the source again for the subscribers it still has, after Dispose dropped its data.
		/// </summary>
		private void ReopenForSubscribers()
		{
			if (_subscriptions.Count > 0)
			{
				Open();
			}
		}
	}
}
