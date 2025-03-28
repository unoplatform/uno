using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.Disposables;
using Windows.UI.Core;
using Uno.UI.Xaml.Media;

#if __IOS__
using UIKit;
using _Image = UIKit.UIImage;
#elif __MACOS__
using AppKit;
using _Image = AppKit.NSImage;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class ImageBrush
	{
		private readonly SerialDisposable _imageScheduler = new SerialDisposable();

		internal event Action<_Image> ImageChanged;

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue)
		{
			if (newValue == null || !newValue.HasSource())
			{
				SetImage(null, failIfNull: false);
				oldValue?.Dispose();
				_imageScheduler.Disposable = null;
			}
			else if (
				newValue.TryOpenSync(out var imageData) &&
				imageData.Kind == ImageDataKind.NativeImage)
			{
				SetImage(imageData.NativeImage);
				oldValue?.Dispose();
				_imageScheduler.Disposable = null;
			}
			else
			{
				SetImage(null, failIfNull: false);
				oldValue?.Dispose();
				OpenImageAsync(newValue);
			}
		}

		private async void OpenImageAsync(ImageSource newValue)
		{
			CoreDispatcher.CheckThreadAccess();

			using var cd = new CancellationDisposable();
			_imageScheduler.Disposable = cd;

			var imageData = await Task.Run(() => newValue.Open(cd.Token), cd.Token);

			if (cd.Token.IsCancellationRequested || imageData.Kind != ImageDataKind.NativeImage)
			{
				return;
			}

			SetImage(imageData.NativeImage);
		}

		private void SetImage(_Image image, bool failIfNull = true)
		{
			ImageChanged?.Invoke(image);

			if (image != null)
			{
				OnImageOpened();
			}
			else if (failIfNull)
			{
				OnImageFailed();
			}
		}
	}
}
