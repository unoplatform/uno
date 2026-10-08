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
		private readonly List<Action<ImageData>> _subscriptions = new List<Action<ImageData>>();

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
		internal IDisposable Subscribe(Action<ImageData> onSourceOpened)
		{
			_subscriptions.Add(onSourceOpened);

			if (_imageData.HasData)
			{
				onSourceOpened(_imageData);
			}
			else if (_subscriptions.Count == 1)
			{
				Open();
			}

			return Disposable.Create(() =>
			{
				_subscriptions.Remove(onSourceOpened);

				// Nothing displays the image anymore: its decoded pixels are released now, as WinUI drops a decoded
				// surface once its last user lets go, and decoded again if something subscribes later.
				if (_subscriptions.Count == 0)
				{
					ReleaseImageData();
				}
			});
		}

		/// <summary>
		/// Number of decoded surfaces released ahead of finalization; for tests.
		/// </summary>
		internal static int ReleasedSurfacesForTesting;

		/// <summary>
		/// Cancels an open in flight and releases the decoded data this source owns.
		/// </summary>
		private void ReleaseImageData()
		{
			_opening.Disposable = null;
			ReleaseSurface(_imageData);
			_imageData = ImageData.Empty;
		}

		/// <summary>
		/// Releases the decoded frames of an image surface that nothing will display, unless the data is shared through
		/// the bitmap cache, in which case it is the cache's to release.
		/// </summary>
		private protected static void ReleaseSurface(ImageData data)
		{
			if (!data.IsShared && data.CompositionSurface is CompositionImageSurface surface)
			{
				surface.ReleaseFrames();
				Interlocked.Increment(ref ReleasedSurfacesForTesting);
			}
		}

		/// <summary>
		/// Indicates that this source has already been opened
		/// (So the onSourceOpened callback of Subscribe will be invoked synchronously!)
		/// </summary>
		internal bool IsOpened => _imageData.HasData;

		private protected void InvalidateSource()
		{
			ReleaseImageData();
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
			try
			{
				if (TryOpenSourceSync(null, null, out var img))
				{
					OnOpened(img);
				}
				else if (TryOpenSourceAsync(ct, null, null, out var asyncImg))
				{
					DispatcherQueue.TryEnqueue(async () =>
					{
						try
						{
							var data = await asyncImg;
							if (!ct.IsCancellationRequested)
							{
								OnOpened(data);
							}
							else
							{
								// A superseded open still produced an image nobody will show: release it now rather than leave
								// it to finalization, which is what a burst of source changes would otherwise pile up.
								ReleaseSurface(data);
							}
						}
						catch (OperationCanceledException) when (ct.IsCancellationRequested)
						{
						}
						catch (Exception error)
						{
							OnOpened(ImageData.FromError(error));
						}
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

			var listeners = _subscriptions.ToList();
			foreach (var listener in listeners)
			{
				listener(data);
			}
		}
	}
}
