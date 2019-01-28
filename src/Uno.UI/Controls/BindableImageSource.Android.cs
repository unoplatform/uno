using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Java.Lang;
using Javax.Xml.Datatype;
using Uno;
using Uno.Extensions;
using Uno.Disposables;
using Uno.Logging;
using Android.Util;
using Android.Graphics;
using Android.Graphics.Drawables;
using System.Threading.Tasks;
using System.Threading;
using Java.Lang.Ref;
using System.IO;
using Math = System.Math;
using System.Drawing;
using Size = System.Drawing.Size;
using System.Reflection;
using ImageLoaderHandler = Uno.UI.Controls.BindableImageView.ImageLoaderHandler;
using Windows.UI.Core;
using Microsoft.Extensions.Logging;

namespace Uno.UI.Controls
{
	/// <summary>
	/// Largely copy-pasted from BindableImageView.
	/// </summary>
	[Windows.UI.Xaml.Data.Bindable]
	internal class BindableImageSource
	{
		public event Action ImageDownloaded = delegate { };

		private string _uriSource;

		private static Type _drawables;
		private static Dictionary<string, int> _drawablesLookup;

		private readonly SerialDisposable _download = new SerialDisposable();
		private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

		/// <summary>
		/// Defines an asynchronous image loader handler that loads a bitmap without assigning it to an ImageView.
		/// </summary>
		/// <param name="ct">A cancellation token</param>
		/// <param name="uri">The image uri</param>
		/// <returns>A Bitmap instance</returns>
		public delegate Task<Bitmap> ImageLoaderHandlerNoImageView(CancellationToken ct, string uri);

		/// <summary>
		/// Provides an optional external image loader.
		/// </summary>
		public static ImageLoaderHandler DefaultImageLoader { get; set; }

		/// <summary>
		/// Provides an optional external image loader that doesn't take an ImageView as a parameter.
		/// </summary>
		public static ImageLoaderHandlerNoImageView DefaultImageLoaderNoImageView { get; set; }

		/// <summary>
		/// An optional image loader for this <see cref="BindableImageSource"/> instance.
		/// </summary>
		public ImageLoaderHandler ImageLoader { get; set; }

		/// <summary>
		/// Provides an optional external image loader that doesn't take an ImageView as a parameter.
		/// </summary>
		public ImageLoaderHandlerNoImageView ImageLoaderNoImageView { get; set; }


		public BindableImageSource()
		{

			UseTargetSize = true;

			ResetImage();
		}

		public string UriSource
		{
			get { return _uriSource; }
			set
			{
				if (_uriSource != value)
				{
					_uriSource = value;
					OnUriSourceChanged();
				}
			}
		}

		public bool UseTargetSize { get; set; }

		public Drawable DrawableSource { get; set; }
		
		public int TargetWidth { get; set; }
		public int TargetHeight { get; set; }

		public int Resource { get; set; }

		public Bitmap CurrentBitmap { get; set; }

		private void ResetImage()
		{
			_download.Disposable = null;
			DrawableSource = null;
		}

		public static Type Drawables
		{
			get
			{
				return _drawables;
			}
			set
			{
				_drawables = value;
				Initialize();
			}
		}
		private static void Initialize()
		{
			_drawablesLookup = _drawables
				.GetFields(BindingFlags.Static | BindingFlags.Public)
				.ToDictionary(
					p => p.Name,
					p => (int)p.GetValue(null)
				);
		}

		private int GetResourceId(string imageName)
		{
			var key = System.IO.Path.GetFileNameWithoutExtension(imageName);
			if (_drawablesLookup == null)
			{
				throw new System.Exception("You must initialize drawable resources by invoking this in your main Module (replace \"GenericApp\"):\nUno.UI.Controls.BindableImageView.Drawables = typeof(GenericApp.Resource.Drawable);");
			}
			var id = _drawablesLookup.UnoGetValueOrDefault(key, 0);
			if (id == 0)
			{
				throw new KeyNotFoundException("Couldn't find drawable with key: " + key);
			}
			return id;
		}

		private void OnUriSourceChanged()
		{
			ResetImage();

			if (!UriSource.HasValue())
			{
				return;
			}
			var newUri = new Uri(UriSource);

			if (newUri.Scheme == "resource")
			{
				Resource = GetResourceId(newUri.PathAndQuery.TrimStart(new[] { '/' }));
			}
			else if (UriSource.StartsWith("res:///", StringComparison.OrdinalIgnoreCase))
			{
				int resourceId;

				if (int.TryParse(UriSource.Replace("res:///", ""), out resourceId))
				{
					Resource = resourceId;
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().WarnFormat("Failed to load asset resource [{0}]", UriSource);
					}
				}
			}
			else if (UriSource.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
			{
				var filePath = UriSource.TrimStart("file://", StringComparison.OrdinalIgnoreCase);

				var options = new BitmapFactory.Options();
				options.InJustDecodeBounds = true;
				BitmapFactory.DecodeFile(filePath, options).SafeDispose();
				var sampleSize = CalculateInSampleSize(options, TargetWidth, TargetHeight);

				options.InJustDecodeBounds = false;
				options.InPurgeable = true;
				options.InSampleSize = sampleSize;

				CurrentBitmap = BitmapFactory.DecodeFile(filePath, options);
			}
			else
			{
#if !IS_UNO
				_download.Disposable = new CoreDispatcherScheduler(CoreDispatcher.Main, CoreDispatcherPriority.Normal)
					.ScheduleAsync((object)null, async (s, state, ct) =>
					{
						var localUri = UriSource;
						
						var b = await DownloadImage(localUri, TargetWidth, TargetHeight, ct);
						if (!ct.IsCancellationRequested && b != null)
						{
							CurrentBitmap = b;
							ImageDownloaded();

							if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
							{
								this.Log().DebugFormat("Bitmap set {0}", localUri);
							}
						}
						else if (b == null && ImageLoader == null)
						{
							// The bitmap may be null if the image loader has already set the image from

							if (this.Log().IsEnabled(LogLevel.Warning))
							{
								this.Log().Warn("Bitmap is null after image download");
							}
						}
					}
				);
#endif
			}
		}

		private async Task<Bitmap> DownloadImage(string uri, int targetWidth, int targetHeight, CancellationToken ct)
		{
			if (ImageLoaderNoImageView != null)
			{
				Size? target = UseTargetSize ? (Size?)new Size(targetWidth, targetHeight) : null;
				
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Using ImageLoader to get {0}", uri);
				}

				return await ImageLoaderNoImageView(ct, uri);
			}
			else
			{
#if !IS_UNO
				return await Schedulers.Default.Run(async ct2 =>
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Initiated download from {0}", uri);
					}

					try
					{
						var q = await new ImageRepository(new Uri(uri), ServiceLocator.Current)
							.Get()
							.ToQuery()
							.Cache(TimeSpan.FromDays(7))
							.ToTask(ct2);

						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().DebugFormat("Image downloaded from {0}", uri); 
						}

						using (var s = q.Stream)
						{
							if (UseTargetSize && targetWidth != 0 && targetHeight != 0)
							{
								using (var ms = new MemoryStream())
								{
									s.CopyTo(ms);
									ms.Position = 0;

									var options = new BitmapFactory.Options();
									options.InJustDecodeBounds = true;
									BitmapFactory.DecodeStream(ms, null, options).SafeDispose();

									var sampleSize = CalculateInSampleSize(options, targetWidth, targetHeight);

									options.InJustDecodeBounds = false;
									options.InPurgeable = true;
									options.InSampleSize = sampleSize;

									ms.Position = 0;

									return BitmapFactory.DecodeStream(ms, null, options);
								}
							}
							else
							{
								return BitmapFactory.DecodeStream(s, null, new BitmapFactory.Options { InPurgeable = true });
							}
						}

					}
					catch (System.Exception ex)
					{
						this.Log().Error("Image download failed", ex);
						return null;
					}
				}, ct);
#else
				throw new NotSupportedException("No image loader specified");
#endif
			}
		}

		public static int CalculateInSampleSize(
			BitmapFactory.Options options, int reqWidth, int reqHeight)
		{
			// Raw height and width of image
			int height = options.OutHeight;
			int width = options.OutWidth;
			int inSampleSize = 1;

			if (height > reqHeight || width > reqWidth)
			{
				int halfHeight = height / 2;
				int halfWidth = width / 2;

				// Calculate the largest inSampleSize value that is a power of 2 and keeps both
				// height and width larger than the requested height and width.
				while ((halfHeight / inSampleSize) > reqHeight
					   && (halfWidth / inSampleSize) > reqWidth)
				{
					inSampleSize *= 2;
				}
			}

			return inSampleSize;
		}

#if !IS_UNO
		private class ImageRepository : IRepository
		{
			public ImageRepository(Uri baseUri, IServiceLocator locator)
			{
				BaseUri = baseUri;
				ServiceLocator = locator;
			}

			public Uri BaseUri { get; private set; }
			public IServiceLocator ServiceLocator { get; private set; }
		}
#endif
	}
}
