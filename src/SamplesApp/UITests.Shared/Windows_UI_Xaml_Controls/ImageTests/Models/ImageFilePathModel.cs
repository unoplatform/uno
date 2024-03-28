using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI;
using Windows.UI.Xaml.Media;
using System.IO;
using Windows.UI;
using Uno.Extensions;
using System.ComponentModel;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using Private.Infrastructure;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
#endif

#if __IOS__
using UIKit;
using CoreGraphics;
using _Bitmap = UIKit.UIImage;
using _Color = UIKit.UIColor;
using Windows.Foundation;
#elif __ANDROID__
using Windows.Foundation;
using _Bitmap = Android.Graphics.Bitmap;
using _Color = Android.Graphics.Color;
#else
using Windows.Foundation;
using _Color = Windows.UI.Color;
#endif

namespace Uno.UI.Samples.UITests.ImageTests.Models
{
	internal class ImageFilePathModel : ViewModelBase
	{
#pragma warning disable CS0109
#if HAS_UNO
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ImageFilePathModel));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(ImageWithLateSourceViewModel));
#endif

#pragma warning restore CS0109

#if HAS_UNO
		private static readonly Size ImageSize = new Size(200, 200);
#endif
		private static readonly _Color ShapeColor = Colors.Tomato;
		private const string StoredFolderName = "SampleImages";
		private const string FileName = "Circle.png";

		private Uri _filePathUri;

		public ImageFilePathModel(UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			var unused = dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, async () =>
			{
				FilePath = await GetAndCreateFilePath(CancellationToken.None);
			});
		}

		private string _filePath;

		public string FilePath
		{
			get => _filePath;
			set
			{
				_filePath = value;
				RaisePropertyChanged();
			}
		}


		private Uri FilePathUri
		{
			get => _filePathUri;
			set
			{
				_filePathUri = value;
				RaisePropertyChanged();
			}
		}

		private
#if HAS_UNO && !__WASM__ && !__SKIA__ && !__MACOS__
			async
#endif
			Task<string> GetAndCreateFilePath(CancellationToken ct)
		{
#if HAS_UNO && !__WASM__ && !__SKIA__ && !__MACOS__
			var bitmap = CreateBitmap();

			_log.Warn(bitmap.ToString());

			var path = await StoreFile(bitmap);
			Uri uri;
			Uri.TryCreate(path.Trim(), UriKind.RelativeOrAbsolute, out uri);
			FilePathUri = uri;
			return uri.AbsoluteUri;
#else
			return Task.FromResult("HAS_UNO-only sample.");
#endif
		}

#if __ANDROID__
		private _Bitmap CreateBitmap()
		{
			var size = ImageSize.LogicalToPhysicalPixels();
			var bitmap = _Bitmap.CreateBitmap((int)size.Width, (int)size.Height, Android.Graphics.Bitmap.Config.Argb8888);
			using (var canvas = new Android.Graphics.Canvas(bitmap))
			{
				var paint = new Android.Graphics.Paint { Color = ShapeColor };
				canvas.DrawCircle((float)(size.Width / 2), (float)(size.Height / 2), (float)(size.Width / 2), paint);
			}
			return bitmap;
		}

		private async Task<string> StoreFile(_Bitmap bitmap)
		{
			var saveFolder = System.IO.Path.Combine(ContextHelper.Current.FilesDir.AbsolutePath, StoredFolderName);
			var savePath = System.IO.Path.Combine(saveFolder, FileName);

			Directory.CreateDirectory(saveFolder);
			using (var stream = new FileStream(savePath, FileMode.OpenOrCreate))
			{
				if (await bitmap.CompressAsync(_Bitmap.CompressFormat.Png, 100, stream))
				{
					return savePath;
				}
				else
				{
					return string.Empty;
				}
			}
		}
#elif __IOS__
		private _Bitmap CreateBitmap()
		{
			UIGraphics.BeginImageContext(ImageSize);
			var context = UIGraphics.GetCurrentContext();
			context.SetFillColor(ShapeColor.CGColor);
			context.FillEllipseInRect(new CGRect(0, 0, ImageSize.Width, ImageSize.Height));
			var bitmap = UIGraphics.GetImageFromCurrentImageContext();

			return bitmap;
		}
		private Task<string> StoreFile(_Bitmap bitmap)
		{
			var saveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), StoredFolderName);
			var savePath = Path.Combine(saveFolder, FileName);

			Directory.CreateDirectory(saveFolder);

			var didSave = bitmap
				.AsPNG()
				.Save(path: savePath, atomically: false);

			return Task.FromResult(didSave ? savePath : string.Empty);
		}
#endif
	}
}
