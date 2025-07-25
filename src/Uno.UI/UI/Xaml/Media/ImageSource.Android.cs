#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Provider;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml.Media;

using ExifInterface = Android.Media.ExifInterface;
using Orientation = Android.Media.Orientation;
using AImageView = Android.Widget.ImageView;

namespace Microsoft.UI.Xaml.Media
{
	public partial class ImageSource
	{
		private const string ContactUriPrefix = "content://com.android.contacts/";

		private static bool _resourceCacheLock;
		private static Dictionary<Tuple<int, global::System.Drawing.Size?>, Bitmap?> _resourceCache = new Dictionary<Tuple<int, global::System.Drawing.Size?>, Bitmap?>();

		private int? _resourceId;

		/// <summary>
		/// Defines an asynchronous image loader handler.
		/// </summary>
		/// <param name="ct">A cancellation token</param>
		/// <param name="uri">The image uri</param>
		/// <param name="targetSize">An optional target decoding size</param>
		/// <returns>A Bitmap instance</returns>
		public delegate Task<Bitmap> ImageLoaderHandler(CancellationToken ct, string uri, AImageView? imageView, global::System.Drawing.Size? targetSize);

		/// <summary>
		/// Provides a optional external image loader.
		/// </summary>
		public static ImageLoaderHandler? DefaultImageLoader { get; set; }

		/// <summary>
		/// An optional image loader for this BindableImageView instance.
		/// </summary>
		public ImageLoaderHandler? ImageLoader { get; set; }

		/// <summary>
		/// The resource path for this ImageSource, if any. Useful for debugging purposes; internally the ResourceId is cached and used when fetching the resource.
		/// </summary>
		internal string? ResourceString { get; private set; }

		internal bool IsImageLoadedToUiDirectly { get; private set; }

		protected ImageSource(Bitmap image) : this()
		{
			if (image is not null)
			{
				_imageData = ImageData.FromBitmap(image);
			}
			else
			{
				_imageData = ImageData.Empty;
			}
		}

		protected ImageSource(BitmapDrawable image) : this()
		{
			BitmapDrawable = image;
		}

		protected ImageSource()
		{
			InitializeBinder();

			UseTargetSize = true;
			InitializeDownloader();
		}

		public bool HasSource()
		{
			return IsSourceReady
				|| Stream != null
				|| AbsoluteUri != null
				|| !FilePath.IsNullOrWhiteSpace()
				|| _imageData.HasData
				|| BitmapDrawable != null
				|| ResourceId != null;
		}

		internal bool ResourceFailed => ResourceString is not null && ResourceId is null;

		internal BitmapDrawable? BitmapDrawable { get; private set; }

		internal int? ResourceId
		{
			get { return _resourceId; }
			private set
			{
				_resourceId = value;
				if (_resourceId != null)
				{
					SetImageLoader();
				}
			}
		}

		static public implicit operator ImageSource(Bitmap image) => new ImageSource(image);

		static public implicit operator ImageSource(BitmapDrawable image) => new ImageSource(image);

		partial void InitFromResource(Uri uri)
		{
			if (
				uri.OriginalString.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
				|| uri.OriginalString.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase))
			{
				// SVGs do not support direct assets reading so we use uri based loading
				AbsoluteUri = uri;
			}
			else
			{
				ResourceString = uri.PathAndQuery.TrimStart('/');

				ResourceId = Uno.Helpers.DrawableHelper.FindResourceIdFromPath(ResourceString);
			}
		}

		partial void CleanupResource()
		{
			ResourceString = null;
			ResourceId = null;
		}

		/// <summary>
		/// Set ImageLoader to be used for this case.
		/// </summary>
		partial void SetImageLoader()
		{
			// Set ImageLoader to be used
			ImageLoader = ImageLoader ?? DefaultImageLoader;
		}

		/// <summary>
		/// Indicates that this source has already been opened (So TryOpenSync will return true!)
		/// </summary>
		internal bool IsOpened => _imageData.HasData;

		/// <summary>
		/// Indicates that this ImageSource has enough information to be opened
		/// </summary>
		private protected virtual bool IsSourceReady => false;

		internal bool TryOpenSync(out Bitmap? image, int? targetWidth = null, int? targetHeight = null)
		{
			if (_imageData.Bitmap is not null)
			{
				image = _imageData.Bitmap;
				return true;
			}

			if (IsSourceReady && TryOpenSourceSync(targetWidth, targetHeight, out var imageData))
			{
				image = imageData.Bitmap;
				return true;
			}

			image = default;
			return false;
		}

		private static Bitmap RespectExifOrientation(ExifInterface exifInterface, Bitmap bitmap)
		{
			int orientation = exifInterface.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);
			var rotationAngle = (Orientation)orientation switch
			{
				Orientation.Rotate270 => 270,
				Orientation.Rotate180 => 180,
				Orientation.Rotate90 => 90,
				_ => 0, // for now, we handle only common orientations.
			};

			if (rotationAngle == 0)
			{
				return bitmap;
			}

			var matrix = new AMatrix();
			matrix.PostRotate(rotationAngle);

			var createdBitmap = Bitmap.CreateBitmap(bitmap, x: 0, y: 0, width: bitmap.Width, height: bitmap.Height, matrix, true);

			// https://developer.android.com/reference/android/graphics/Bitmap#createBitmap(android.util.DisplayMetrics,%20int,%20int,%20android.graphics.Bitmap.Config,%20boolean,%20android.graphics.ColorSpace)
			// Bitmap.CreateBitmap is marked as nullable, but it never returns null.
			return createdBitmap!;
		}

		private static async Task<Bitmap?> DecodeStreamAsBitmapWithExifOrientation(Stream stream, Rect? outPadding = null, BitmapFactory.Options? options = null)
		{
			if (await BitmapFactory.DecodeStreamAsync(stream, outPadding, options) is not { } bitmap)
			{
				return null;
			}

			if (!stream.CanSeek)
			{
				// DecodeStreamAsync have read to the end, if we can't reset
				// the Position to zero, ExifInterface will not be able to read the orientation correctly.
				return bitmap;
			}

			stream.Position = 0;

			var exifInterface = new ExifInterface(stream);

			return RespectExifOrientation(exifInterface, bitmap);
		}

		private static async Task<Bitmap?> DecodeFileAsBitmapWithExifOrientation(string path, BitmapFactory.Options? options = null)
		{
			if (await BitmapFactory.DecodeFileAsync(path, options) is not { } bitmap)
			{
				return null;
			}

			var exifInterface = new ExifInterface(path);
			return RespectExifOrientation(exifInterface, bitmap);
		}

		internal async Task<ImageData> Open(CancellationToken ct, AImageView? targetImage, int? targetWidth = null, int? targetHeight = null)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + $" Open(tw:{targetWidth}x{targetHeight})");
			}

			IsImageLoadedToUiDirectly = false;

			var targetSize = UseTargetSize && targetWidth != null && targetHeight != null
				? (global::System.Drawing.Size?)new global::System.Drawing.Size(targetWidth.Value, targetHeight.Value)
				: null;

			if (IsSourceReady && TryOpenSourceSync(targetWidth, targetHeight, out var img))
			{
				return _imageData = img;
			}

			if (IsSourceReady && TryOpenSourceAsync(ct, targetWidth, targetHeight, out var asyncImg))
			{
				return _imageData = await asyncImg;
			}

			if (ResourceId.HasValue)
			{
				return _imageData = await FetchResourceWithDownsampling(ct, ResourceId.Value, targetSize);
			}

			BitmapFactory.Options options = new BitmapFactory.Options();
			options.InJustDecodeBounds = true;

			if (Stream != null)
			{
				if (Stream.CanSeek)  //For now if we can only validate the size of streams that are seekable
				{
					var emptyPadding = new Rect();
					// Try to move position to beginning of the stream, if seekable.
					Stream.Position = 0;
					//Get the size of the image and validate
					await BitmapFactory.DecodeStreamAsync(Stream, emptyPadding, options);
					Stream.Position = 0;
					if (ValidateIfImageNeedsResize(options))
					{
						options.InJustDecodeBounds = false;
						return _imageData = ImageData.FromBitmap(await DecodeStreamAsBitmapWithExifOrientation(Stream, emptyPadding, options));
					}
				}

				return _imageData = ImageData.FromBitmap(await DecodeStreamAsBitmapWithExifOrientation(Stream));
			}

			if (!FilePath.IsNullOrEmpty())
			{
				await BitmapFactory.DecodeFileAsync(FilePath, options);
				if (ValidateIfImageNeedsResize(options))
				{
					options.InJustDecodeBounds = false;
					return _imageData = ImageData.FromBitmap(await DecodeFileAsBitmapWithExifOrientation(FilePath, options));
				}
				return _imageData = ImageData.FromBitmap(await DecodeFileAsBitmapWithExifOrientation(FilePath));
			}

			if (AbsoluteUri != null)
			{
				if (ImageLoader == null)
				{
					// The ContactsService returns the contact uri for compatibility with UniversalImageLoader - in order to obtain the corresponding photo we resolve using the service below.
					if (IsContactUri(AbsoluteUri))
					{
						if (ContactsContract.Contacts.OpenContactPhotoInputStream(ContextHelper.Current.ContentResolver, AUri.Parse(AbsoluteUri.OriginalString)) is not { } stream)
						{
							return _imageData = ImageData.Empty;
						}

						return _imageData = ImageData.FromBitmap(await DecodeStreamAsBitmapWithExifOrientation(stream));
					}

					var filePath = await Download(ct, AbsoluteUri);

					if (filePath == null)
					{
						return ImageData.Empty;
					}

					return _imageData = ImageData.FromBitmap(await DecodeFileAsBitmapWithExifOrientation(filePath.LocalPath));
				}
				else
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Using ImageLoader to get {0}", AbsoluteUri);
					}

					_imageData = ImageData.FromBitmap(await ImageLoader(ct, AbsoluteUri.OriginalString, targetImage, targetSize));

					if (
						!ct.IsCancellationRequested
						&& targetImage != null
						&& targetSize == null)
					{
						IsImageLoadedToUiDirectly = true;

						targetImage.SetImageBitmap(_imageData.Bitmap);
					}

					return _imageData;
				}
			}

			return ImageData.Empty;
		}

		private bool ValidateIfImageNeedsResize(BitmapFactory.Options options)
		{
			if (options.OutHeight > 4096 || options.OutWidth > 4096)
			{
				options.InSampleSize = CalculateInSampleSize(options, 4096, 4096);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Source of code taken from https://developer.android.com/training/displaying-bitmaps/load-bitmap.html
		/// with small adjustment
		/// </summary>
		/// <param name="options"></param>
		/// <param name="reqWidth"></param>
		/// <param name="reqHeight"></param>
		/// <returns></returns>
		private static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
		{
			// Raw height and width of image
			int height = options.OutHeight;
			int width = options.OutWidth;
			int inSampleSize = 2;

			if (height > reqHeight || width > reqWidth)
			{

				int halfHeight = height / 2;
				int halfWidth = width / 2;

				// Calculate the largest inSampleSize value that is a power of 2 and keeps both
				// height and width larger than the requested height and width.
				while ((halfHeight / inSampleSize) >= reqHeight
				&& (halfWidth / inSampleSize) >= reqWidth)
				{
					inSampleSize *= 2;
				}
			}

			return inSampleSize;
		}

		internal static bool IsContactUri(Uri uri)
		{
			return uri?.OriginalString.StartsWith(ContactUriPrefix, StringComparison.OrdinalIgnoreCase) ?? false;
		}

		/// <summary>
		/// Asynchronously retrieves an image resource, scaled to the target dimensions.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="resourceId"></param>
		/// <param name="targetSize"></param>
		/// <returns></returns>
		private async Task<ImageData> FetchResourceWithDownsampling(CancellationToken ct, int resourceId, global::System.Drawing.Size? targetSize)
		{
			var key = Tuple.Create(resourceId, targetSize);

			Bitmap? bitmap;
			if (!_resourceCache.TryGetValue(key, out bitmap))
			{
				try
				{
					while (_resourceCacheLock)
					{
						await Task.Yield();
					}

					_resourceCacheLock = true;

					if (!_resourceCache.TryGetValue(key, out bitmap))
					{
						var options = new BitmapFactory.Options();
						if (targetSize.HasValue)
						{
							//Get image bounds only
							options.InJustDecodeBounds = true;
							BitmapFactory.DecodeResource(ContextHelper.Current.Resources, resourceId, options);

							//Get downsampling to use. Note we're not worried about rounding, DecodeResource will round down to nearest power of 2 anyway - http://developer.android.com/training/displaying-bitmaps/load-bitmap.html
							float scale = options.InDensity != 0 && options.InTargetDensity != 0 ?
								//http://stackoverflow.com/questions/15167794/bitmapfactory-optionsoutwidth-returns-different-size-than-actual-bitmap
								(float)options.InTargetDensity / (float)options.InDensity :
								1;
							options.InSampleSize = (int)Math.Min(options.OutWidth * scale / targetSize.Value.Width, options.OutHeight * scale / targetSize.Value.Height);

							options.InJustDecodeBounds = false;
						}

						bitmap = await BitmapFactory.DecodeResourceAsync(ContextHelper.Current.Resources, resourceId, options);

						_resourceCache.Add(key, bitmap);
					}
				}
				finally
				{
					_resourceCacheLock = false;
				}
			}

			if (bitmap is not null)
			{
				return ImageData.FromBitmap(bitmap);
			}
			else
			{
				return ImageData.Empty;
			}
		}

		#region Resources

		/// <summary>
		/// Type used as the source of Drawables.
		/// Now available outside in Uno library with <see cref="DrawableHelper"/>.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Type? Drawables
		{
			get => Uno.Helpers.DrawableHelper.Drawables;
			set => Uno.Helpers.DrawableHelper.Drawables = value;
		}

		/// <summary>
		/// Returns the Id of the bundled image.
		/// Now available outside in Uno library with <see cref="DrawableHelper"/>.
		/// </summary>
		/// <param name="imageName">Name of the image</param>
		/// <returns>Resource's id</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static int? FindResourceId(string imageName) =>
			Uno.Helpers.DrawableHelper.FindResourceId(imageName);

		#endregion

		partial void DisposePartial()
		{
			if (BitmapDrawable != null)
			{
				BitmapDrawable.Dispose();
				BitmapDrawable = null;
			}
		}

		partial void UnloadImageDataPlatform()
		{
			UnloadBitmapImageData();
		}

		private void UnloadBitmapImageData()
		{
			if (_imageData.Bitmap is not null)
			{
				if (_imageData.Bitmap.Handle != IntPtr.Zero)
				{
					_imageData.Bitmap.Recycle();
				}
				else if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Attempting to dispose {nameof(_imageData)} when the native bitmap has already been collected.");
				}
				_imageData.Bitmap.Dispose();
			}
		}

		public override string ToString()
		{
			var source = Stream ?? AbsoluteUri ?? FilePath ?? _imageData.Bitmap ?? (object?)BitmapDrawable ?? ResourceString ?? "[No source]";
			return "ImageSource: {0}".InvariantCultureFormat(source);
		}
	}
#nullable disable

	public class Converter : TypeConverter
	{
		// Overrides the CanConvertFrom method of TypeConverter.
		// The ITypeDescriptorContext interface provides the context for the
		// conversion. Typically, this interface is used at design time to
		// provide information about the design-time container.
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(string))
			{
				return true;
			}
			if (sourceType == typeof(Uri))
			{
				return true;
			}
			if (sourceType == typeof(Bitmap))
			{
				return true;
			}
			if (sourceType == typeof(BitmapDrawable))
			{
				return true;
			}
			if (sourceType.Is(typeof(Stream)))
			{
				return true;
			}

			return base.CanConvertFrom(context, sourceType);
		}

		// Overrides the ConvertFrom method of TypeConverter.
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string)
			{
				return (ImageSource)value;
			}
			if (value is Uri)
			{
				return (ImageSource)value;
			}
			if (value is Bitmap)
			{
				return (ImageSource)value;
			}
			if (value is BitmapDrawable)
			{
				return (ImageSource)value;
			}
			if (value is Stream)
			{
				return (ImageSource)value;
			}

			return base.ConvertFrom(context, culture, value);
		}

		// Overrides the ConvertTo method of TypeConverter.
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
