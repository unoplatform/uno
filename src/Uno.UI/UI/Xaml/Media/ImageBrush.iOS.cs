using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Views.Controls;
using Uno.UI.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using UIKit;
using Uno.Disposables;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageBrush
	{
		private readonly SerialDisposable _imageScheduler = new SerialDisposable();

		internal event Action<UIImage> ImageChanged;

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue)
		{
			if (newValue == null || !newValue.HasSource())
			{
				SetImage(null, failIfNull: false);
				oldValue?.Dispose();
				_imageScheduler.Disposable = null;
			}
			else if (newValue.ImageData != null)
			{
				SetImage(newValue.ImageData);
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

			var cd = new CancellationDisposable();

			_imageScheduler.Disposable = cd;

			var image = await Task.Run(() => newValue.Open(cd.Token));

			if (cd.Token.IsCancellationRequested)
			{
				return;
			}

			SetImage(image);
		}

		private void SetImage(UIImage image, bool failIfNull = true)
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
