using Uno.Extensions;
using Uno.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Windows.UI.Xaml.Media
{
	partial class ImageSource
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = global::System.Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

		private readonly SerialDisposable _opening = new SerialDisposable();
		private readonly List<Action<ImageData>> _subscriptions = new List<Action<ImageData>>();

		private ImageData? _cache;

		partial void InitFromResource(Uri uri)
		{
			WebUri = new Uri(uri.PathAndQuery.TrimStart("/"), UriKind.Relative);
		}

		/// <summary>
		/// Subscribes to this image source
		/// </summary>
		/// <param name="onSourceOpened">
		/// A callback that will be invoked each time the source has been updated
		/// (i.e. a property has changed on the source AND the source has been re-opened)
		/// </param>
		/// <param name="targetWidth">An optional width that **can** be used by the source to decode the source directly at the right size (for perf considerations)</param>
		/// <param name="targetHeight">An optional height that **can** be used by the source to decode the source directly at the right size (for perf considerations)</param>
		internal IDisposable Subscribe(Action<ImageData> onSourceOpened, int? targetWidth = null, int? targetHeight = null)
		{
			_subscriptions.Add(onSourceOpened);

			if (_cache.HasValue)
			{
				onSourceOpened(_cache.Value);
			}
			else if (_subscriptions.Count == 1)
			{
				RequestOpen(targetWidth, targetHeight);
			}

			return Disposable.Create(() => _subscriptions.Remove(onSourceOpened));
		}

		/// <summary>
		/// Indicates that this source has already been opened
		/// (So the onSourceOpened callback of Subscribe will be invoked synchronously!)
		/// </summary>
		internal bool IsOpened => _cache.HasValue;

		#region Implementers API
		private protected virtual bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			// Unlike other platforms, on WASM all the legacy sources are handled synchronously.
			return TryOpenSourceLegacy(out image);
		}

		private protected virtual bool TryOpenSourceAsync(int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		{
			asyncImage = default;
			return false;
		}

		private protected void InvalidateSource()
		{
			_cache = null;
			if (_subscriptions.Count > 0)
			{
				RequestOpen();
			}
		}
		#endregion

		private void RequestOpen(int? targetWidth = null, int? targetHeight = null)
		{
			try
			{
				if (TryOpenSourceSync(targetWidth, targetHeight, out var img))
				{
					OnOpened(img);
				}
				else
				{
					_opening.Disposable = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, ct => Open(ct, targetWidth, targetHeight));
				}
			}
			catch (Exception error)
			{
				OnOpened(new ImageData
				{
					Kind = ImageDataKind.Error,
					Error = error
				});
			}
		}

		private async Task Open(CancellationToken ct, int? targetWidth = null, int? targetHeight = null)
		{
			try
			{
				if (TryOpenSourceSync(targetWidth, targetHeight, out var img))
				{
					OnOpened(img);
				}
				else if (TryOpenSourceAsync(targetWidth, targetHeight, out var asyncImg))
				{
					OnOpened(await asyncImg);
				}
				else
				{
					OnOpened(new ImageData()); // Empty
				}
			}
			catch (Exception error)
			{
				OnOpened(new ImageData
				{
					Kind = ImageDataKind.Error,
					Error = error
				});
			}
		}

		private void OnOpened(ImageData data)
		{
			_cache = data; // We should also cache the targetWidth and targetHeight

			var listeners = _subscriptions.ToList();
			foreach (var listener in listeners)
			{
				listener(data);
			}
		}

		// TODO: All those should be implemented by sub-classes in TryOpenSource<Sync|Async> overloads!
		private bool TryOpenSourceLegacy(out ImageData img)
		{
			var stream = Stream;
			if (stream != null)
			{
				stream.Position = 0;
				var encodedBytes = Convert.ToBase64String(stream.ReadBytes());

				img = new ImageData
				{
					Kind = ImageDataKind.Base64,
					Value = "data:application/octet-stream;base64," + encodedBytes
				};
				return true;
			}

			var url = WebUri;
			if (url != null)
			{
				string value;
				if (url.IsAbsoluteUri)
				{
					value = url.Scheme switch
					{
						// Local files are assumed as coming from the remoter server
						"file" when UNO_BOOTSTRAP_APP_BASE == null => url.PathAndQuery,
						"file" => UNO_BOOTSTRAP_APP_BASE + "/" + url.PathAndQuery,
						_ => url.AbsoluteUri
					};
				}
				else
				{
					value = UNO_BOOTSTRAP_APP_BASE == null
						? url.OriginalString
						: UNO_BOOTSTRAP_APP_BASE + "/" + url.OriginalString;
				}
					

				img = new ImageData
				{
					Kind = ImageDataKind.Url,
					Value = value
				};
				return true;
			}

			img = default;
			return false;
		}
	}
}
