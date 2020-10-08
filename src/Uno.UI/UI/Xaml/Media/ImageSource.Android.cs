using Android.Graphics;
using Android.Graphics.Drawables;
using Uno.Extensions;
using Uno.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Provider;
using Microsoft.Extensions.Logging;
using Uno.UI;
using Uno;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageSource
	{
		private const string DrawableUriPrefix = "drawable://";

		private const string ContactUriPrefix = "content://com.android.contacts/";

		private static bool _resourceCacheLock;
		private static Dictionary<Tuple<int, global::System.Drawing.Size?>, Bitmap> _resourceCache = new Dictionary<Tuple<int, global::System.Drawing.Size?>, Bitmap>();

		private int? _resourceId;

		/// <summary>
		/// Defines an asynchronous image loader handler.
		/// </summary>
		/// <param name="ct">A cancellation token</param>
		/// <param name="uri">The image uri</param>
		/// <param name="targetSize">An optional target decoding size</param>
		/// <returns>A Bitmap instance</returns>
		public delegate Task<Bitmap> ImageLoaderHandler(CancellationToken ct, string uri, Android.Widget.ImageView imageView, global::System.Drawing.Size? targetSize);

		/// <summary>
		/// Provides a optional external image loader.
		/// </summary>
		public static ImageLoaderHandler DefaultImageLoader { get; set; }

		/// <summary>
		/// An optional image loader for this BindableImageView instance.
		/// </summary>
		public ImageLoaderHandler ImageLoader { get; set; }

		/// <summary>
		/// The resource path for this ImageSource, if any. Useful for debugging purposes; internally the ResourceId is cached and used when fetching the resource.
		/// </summary>
		internal string ResourceString { get; private set; }

		internal bool IsImageLoadedToUiDirectly { get; private set; }

		protected ImageSource(Bitmap image) : this()
		{
			_imageData = image;
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
				|| WebUri != null
				|| FilePath.HasValueTrimmed()
				|| _imageData != null
				|| BitmapDrawable != null
				|| ResourceId != null;
		}

		private Bitmap _imageData;
		internal BitmapDrawable BitmapDrawable { get; private set; }

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

		static public implicit operator ImageSource(Bitmap image)
		{
			return new ImageSource(image);
		}

		static public implicit operator ImageSource(BitmapDrawable image)
		{
			return new ImageSource(image);
		}

		partial void InitFromResource(Uri uri)
		{
			ResourceString = uri.PathAndQuery.TrimStart(new[] { '/' });
			ResourceId = Uno.Helpers.DrawableHelper.FindResourceIdFromPath(ResourceString);
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
		internal bool IsOpened => _imageData != null;

		/// <summary>
		/// Indicates that this ImageSource has enough information to be opened
		/// </summary>
		private protected virtual bool IsSourceReady => false;

		private protected virtual bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out Bitmap image)
		{
			image = default;
			return false;
		}

		private protected virtual bool TryOpenSourceAsync(int? targetWidth, int? targetHeight, out Task<Bitmap> asyncImage)
		{
			asyncImage = default;
			return false;
		}

		internal bool TryOpenSync(out Bitmap image, int? targetWidth = null, int? targetHeight = null)
		{
			if (_imageData != null)
			{
				image = _imageData;
				return true;
			}

			if (IsSourceReady && TryOpenSourceSync(targetWidth, targetHeight, out image))
			{
				return true;
			}

			image = default;
			return false;
		}

		internal async Task<Bitmap> Open(CancellationToken ct, Android.Widget.ImageView targetImage = null, int? targetWidth = null, int? targetHeight = null)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(this.ToString() + $" Open(tw:{targetWidth}x{targetHeight})");
			}

			IsImageLoadedToUiDirectly = false;

			BitmapFactory.Options options = new BitmapFactory.Options();
			options.InJustDecodeBounds = true;

			var targetSize = UseTargetSize && targetWidth != null && targetHeight != null
				? (global::System.Drawing.Size?)new global::System.Drawing.Size(targetWidth.Value, targetHeight.Value)
				: null;

			if (IsSourceReady && TryOpenSourceSync(targetWidth, targetHeight, out var img))
			{
				return _imageData = img;
			}

			if (IsSourceReady && TryOpenSourceAsync(targetWidth, targetHeight, out var asyncImg))
			{
				return _imageData = await asyncImg;
			}

			if (ResourceId.HasValue)
			{
				return _imageData = await FetchResourceWithDownsampling(ct, ResourceId.Value, targetSize);
			}

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
						return _imageData = await BitmapFactory.DecodeStreamAsync(Stream, emptyPadding, options);
					}
				}

				return _imageData = await BitmapFactory.DecodeStreamAsync(Stream);
			}

			if (FilePath.HasValue())
			{
				await BitmapFactory.DecodeFileAsync(FilePath, options);
				if (ValidateIfImageNeedsResize(options))
				{
					options.InJustDecodeBounds = false;
					return _imageData = await BitmapFactory.DecodeFileAsync(FilePath, options);
				}
				return _imageData = await BitmapFactory.DecodeFileAsync(FilePath);
			}

			if (WebUri != null)
			{
				if (ImageLoader == null)
				{
					// The ContactsService returns the contact uri for compatibility with UniversalImageLoader - in order to obtain the corresponding photo we resolve using the service below.
					if (IsContactUri(WebUri))
					{
						var stream = ContactsContract.Contacts.OpenContactPhotoInputStream(ContextHelper.Current.ContentResolver, Android.Net.Uri.Parse(WebUri.OriginalString));

						return _imageData = await BitmapFactory.DecodeStreamAsync(stream);
					}

					var filePath = await Download(ct, WebUri);

					if (filePath == null)
					{
						return null;
					}

					return _imageData = await BitmapFactory.DecodeFileAsync(filePath.LocalPath);
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Using ImageLoader to get {0}", WebUri);
					}

					_imageData = await ImageLoader(ct, WebUri.OriginalString, targetImage, targetSize);

					if (
						!ct.IsCancellationRequested
						&& targetImage != null
						&& targetSize == null
						)
					{
						IsImageLoadedToUiDirectly = true;

						targetImage.SetImageBitmap(_imageData);
					}

					return _imageData;
				}
			}

			return null;
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

		private static bool IsContactUri(Uri uri)
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
		private async Task<Bitmap> FetchResourceWithDownsampling(CancellationToken ct, int resourceId, global::System.Drawing.Size? targetSize)
		{
			var key = Tuple.Create(resourceId, targetSize);

			Bitmap bitmap;
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

			return bitmap;
		}

		#region Resources

		/// <summary>
		/// Type used as the source of Drawables.
		/// Now available outside in Uno library with <see cref="DrawableHelper"/>.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Type Drawables
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
			UnloadImageData();
			if (BitmapDrawable != null)
			{
				BitmapDrawable.Dispose();
				BitmapDrawable = null;
			}
		}

		internal void UnloadImageData()
		{
			if (_imageData != null)
			{
				if (_imageData.Handle != IntPtr.Zero)
				{
					_imageData.Recycle();
				}
				else if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Attempting to dispose {nameof(_imageData)} when the native bitmap has already been collected.");
				}
				_imageData.Dispose();
				_imageData = null;
			}
		}

		public override string ToString()
		{
			var source = Stream ?? WebUri ?? FilePath ?? _imageData ?? (object)BitmapDrawable ?? ResourceString ?? "[No source]";
			return "ImageSource: {0}".InvariantCultureFormat(source);
		}

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
}
