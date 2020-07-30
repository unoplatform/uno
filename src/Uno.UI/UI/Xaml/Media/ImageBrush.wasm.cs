using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageBrush
	{
		private readonly SerialDisposable _sourceSubscription = new SerialDisposable();
		private readonly List<Action<ImageData>> _listeners = new List<Action<ImageData>>();

		private ImageData? _cache;

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue)
		{
			_cache = default;
			_sourceSubscription.Disposable = Disposable.Empty;

			if (newValue != null && _listeners.Count > 0)
			{
				_sourceSubscription.Disposable = newValue.Subscribe(OnSourceOpened);
			}
		}

		internal IDisposable Subscribe(Action<ImageData> onUpdated)
		{
			_listeners.Add(onUpdated);

			if (_cache.HasValue)
			{
				onUpdated(_cache.Value);
			}

			var source = ImageSource;
			if (source != null && _listeners.Count == 1)
			{
				_sourceSubscription.Disposable = source.Subscribe(OnSourceOpened);
			}

			return Disposable.Create(() =>
			{
				_listeners.Remove(onUpdated);
				if (_listeners.Count == 0)
				{
					_sourceSubscription.Disposable = Disposable.Empty;
				}
			});
		}

		private void OnSourceOpened(ImageData image)
		{
			_cache = image;

			var listeners = _listeners.ToList();
			foreach (var listener in listeners)
			{
				listener(image);
			}

			if (image.Kind == ImageDataKind.Error)
			{
				// Note: On WASM, images are loaded by the platform (we only propagate the url),
				//		 so the ImageFailed won't be raised if the url is invalid ...
				OnImageFailed();
			}
			else
			{
				OnImageOpened();
			}
		}
	}
}
