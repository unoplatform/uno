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
using Windows.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Devices.Enumeration;
using Uno.UI.Xaml.Media;


#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Windows.UI.Xaml.Media
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
				RequestOpen();
			}

			return Disposable.Create(() => _subscriptions.Remove(onSourceOpened));
		}

		/// <summary>
		/// Indicates that this source has already been opened
		/// (So the onSourceOpened callback of Subscribe will be invoked synchronously!)
		/// </summary>
		internal bool IsOpened => _imageData.HasData;

		private protected void InvalidateSource()
		{
			_imageData = default;
			if (_subscriptions.Count > 0 || this is SvgImageSource)
			{
				RequestOpen();
			}
		}

		private protected void RequestOpen()
		{
			try
			{
				if (TryOpenSourceSync(null, null, out var img))
				{
					OnOpened(img);
				}
				else
				{
					_opening.Disposable = null;

					_opening.Disposable = Uno.UI.Dispatching.NativeDispatcher.Main.EnqueueCancellableOperation(
						ct => _ = Open(ct));
				}
			}
			catch (Exception error)
			{
				this.Log().Error($"Error loading image: {error}");
				OnOpened(ImageData.FromError(error));
			}
		}

		private async Task Open(CancellationToken ct)
		{
			try
			{
				if (TryOpenSourceSync(null, null, out var img))
				{
					OnOpened(img);
				}
				else if (TryOpenSourceAsync(ct, null, null, out var asyncImg))
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
