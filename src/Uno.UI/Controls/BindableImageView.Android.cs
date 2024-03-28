using System;
using System.Collections.Generic;
using System.Globalization;
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
using Uno.Foundation.Logging;
using Android.Util;
using Android.Graphics;
using Android.Graphics.Drawables;
using System.Threading.Tasks;
using System.Threading;
using Java.Lang.Ref;
using System.IO;
using Math = System.Math;
using Size = System.Drawing.Size;
using System.Reflection;
using Windows.UI.Core;
using Uno.Helpers;

#pragma warning disable CS0649 // TODO: Fix.

namespace Uno.UI.Controls
{
	[Windows.UI.Xaml.Data.Bindable]
	public class BindableImageView : ImageView, View.IOnTouchListener
	{
		private string _uriSource;

		private static Type _drawables;
		private static Dictionary<string, int> _drawablesLookup;

		private readonly SerialDisposable _download = new SerialDisposable();
		private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
		private readonly bool _isInteractive;
		private readonly bool _detachFromWindow;
		private readonly bool _attachToWindow;

		/// <summary>
		/// Defines an asynchronous image loader handler.
		/// </summary>
		/// <param name="ct">A cancellation token</param>
		/// <param name="uri">The image uri</param>
		/// <param name="targetSize">An optional target decoding size</param>
		/// <returns>A Bitmap instance</returns>
		public delegate Task<Bitmap> ImageLoaderHandler(CancellationToken ct, string uri, ImageView imageView, Size? targetSize);

		/// <summary>
		/// Provides a optional external image loader.
		/// </summary>
		public static ImageLoaderHandler DefaultImageLoader { get; set; }

		/// <summary>
		/// An optional image loader for this BindableImageView instance.
		/// </summary>
		public ImageLoaderHandler ImageLoader { get; set; }

		#region Interaction fields

		private Matrix _matrix;

		private ManipulationMode _manipulationMode = ManipulationMode.None;

		//Zoom
		private System.Drawing.PointF _previousPoint;
		private System.Drawing.PointF _manipulationStart;
		private float[] _m;

		//Scaling
		private int _viewHeight, _viewWidth;
		private float _originalWidth, _originalHeight;
		private int _previousMeasuredHeight, _previousMeasuredWidth;
		private float _saveScale = 1f;

		//private ScaleGestureDetector _scaleDetector;
		//private GestureDetector _doubleTapDetector;

		private static int DeltaClick = 3;
		private Bitmap _currentBitmap;
		private int _targetWidth;
		private int _targetHeight;
		private bool _sourceUpdateRequired;

		public float MinZoom { get; private set; }

		public float MaxZoom { get; set; }

		#endregion

		/// <summary>
		/// Internal use.
		/// </summary>
		/// <remarks>This constructor is *REQUIRED* for the Mono/Java
		/// binding layer to function properly, in case java objects need to call methods
		/// on a collected .NET instance.
		/// </remarks>
		internal BindableImageView(System.IntPtr ptr, Android.Runtime.JniHandleOwnership ownership)
			: base(ptr, ownership)
		{
		}

		public BindableImageView(Android.Content.Context context)
			: base(context)
		{
			_isInteractive = false;
			_attachToWindow = true;
			_detachFromWindow = true;
			ImageLoader = DefaultImageLoader;

			UseTargetSize = true;

			this.RegisterViewAttachedStateChanged(
				_ => AttachedToWindow(),
				_ => DetachedFromWindow()
			).DisposeWith(_subscriptions);
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

		//Disabled for now as it breaks the support for bitmap source in the Image control.
		//To put back in place when Image and BindableImageView are merged together
		private void DetachedFromWindow()
		{
			if (_detachFromWindow && UriSource != null)
			{
				ResetImage();
			}
		}

		private void AttachedToWindow()
		{
			if (_attachToWindow && UriSource != null)
			{
				OnUriSourceChanged();
			}
		}

		public Drawable DrawableSource
		{
			get { return Drawable; }
			set
			{
				SetImageDrawable(value);
				OnDrawableChanged();
			}
		}

		private void ResetImage()
		{
			_download.Disposable = null;
			SetImageDrawable(null);
		}

		public override void SetImageBitmap(Bitmap bm)
		{
			_currentBitmap = bm;
			base.SetImageBitmap(bm);
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
			if (Parent == null)
			{
				// If the parent is not present we can wait for OnAttachedToWindow to call again.
				return;
			}

			ResetImage();

			if (UriSource.IsNullOrEmpty())
			{
				return;
			}
			var newUri = new Uri(UriSource);

			if (newUri.Scheme == "resource"
				|| newUri.IsFile
				|| newUri.IsLocalResource())
			{
				SetImageResource(GetResourceId(newUri.PathAndQuery.TrimStart('/')));
			}
			else if (UriSource.StartsWith("res:///", StringComparison.OrdinalIgnoreCase))
			{
				int resourceId;

				if (int.TryParse(UriSource.Replace("res:///", ""), CultureInfo.InvariantCulture, out resourceId))
				{
					SetImageResource(resourceId);
				}
				else
				{
					this.Log().Warn($"Failed to load asset resource [{UriSource}]");
				}
			}
			else if (UriSource.StartsWith("file://", StringComparison.OrdinalIgnoreCase) || newUri.IsAppData())
			{
				if (_targetHeight <= 0 || _targetWidth <= 0)
				{
					_sourceUpdateRequired = true;
					return;
				}

				var filePath = newUri.IsAppData() ?
					AppDataUriEvaluator.ToPath(newUri) :
					UriSource.TrimStart("file://", ignoreCase: true);

				var options = new BitmapFactory.Options();
				options.InJustDecodeBounds = true;
				BitmapFactory.DecodeFile(filePath, options).SafeDispose();
				var sampleSize = CalculateInSampleSize(options, _targetWidth, _targetHeight);

				options.InJustDecodeBounds = false;
#pragma warning disable CS0618 // Type or member is obsolete
				options.InPurgeable = true;
#pragma warning restore CS0618 // Type or member is obsolete
				options.InSampleSize = sampleSize;

				var bitmap = BitmapFactory.DecodeFile(filePath, options);

				if (_currentBitmap != bitmap)
				{
					SetImageBitmap(bitmap);
				}
			}
			else
			{
				_download.Disposable = Uno.UI.Dispatching.NativeDispatcher.Main
					.EnqueueCancellableOperation(
						async (ct) =>
						{
							var localUri = UriSource;

							using (var b = await DownloadImage(localUri, _targetWidth, _targetHeight, ct))
							{
								if (!ct.IsCancellationRequested && b != null)
								{
									if (_currentBitmap != b)
									{
										SetImageBitmap(b);
									}

									if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
									{
										this.Log().DebugFormat("Bitmap set {0}", localUri);
									}

									if (_isInteractive)
									{
										_saveScale = 1f;
										OnDrawableChanged();
									}
								}
								else if (b == null && ImageLoader == null)
								{
									// The bitmap may be null if the image loader has already set the image from

									this.Log().Warn("Bitmap is null after image download");
								}
							}
						}
				);
			}
		}

		private async Task<Bitmap> DownloadImage(string uri, int targetWidth, int targetHeight, CancellationToken ct)
		{
			if (ImageLoader != null)
			{
				Size? target = UseTargetSize ? (Size?)new Size(targetWidth, targetHeight) : null;

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Using ImageLoader to get {0}", uri);
				}

				return await ImageLoader(ct, uri, this, target);
			}
			else
			{
#if !IS_UNO
				return await Schedulers.Default.Run(async ct2 =>
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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

						this.Log().DebugFormat("Image downloaded from {0}", uri);

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
				throw new NotSupportedException("No imageloader specified");
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

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			_targetWidth = ViewHelper.MeasureSpecGetSize(widthMeasureSpec);
			_targetHeight = ViewHelper.MeasureSpecGetSize(heightMeasureSpec);

			if (_isInteractive)
			{
				_viewWidth = Width;
				_viewHeight = Height;

				//
				// Rescales image on rotation
				//
				if (_previousMeasuredWidth == _viewWidth
					&& _previousMeasuredHeight == _viewHeight
					|| _viewWidth == 0
					|| _viewHeight == 0)
				{
					return;
				}
				_previousMeasuredWidth = _viewWidth;
				_previousMeasuredHeight = _viewHeight;

				OnDrawableChanged();

				FixTrans();
			}

			if (_sourceUpdateRequired)
			{
				_sourceUpdateRequired = false;
				OnUriSourceChanged();
			}
		}

		#region Interaction methods

		private void OnDrawableChanged()
		{
			if (_matrix != null && _saveScale == 1 && Width != 0 && Height != 0)
			{
				var drawable = Drawable;
				if (drawable == null
					|| drawable.IntrinsicWidth == 0
					|| drawable.IntrinsicHeight == 0)
				{
					return;
				}

				// Fit to screen.
				var bitmapWidth = drawable.IntrinsicWidth;
				var bitmapHeight = drawable.IntrinsicHeight;

				var scaleX = (float)Width / (float)bitmapWidth;
				var scaleY = (float)Height / (float)bitmapHeight;

				var scale = scaleX == 0 ? scaleY : (scaleY == 0 ? scaleX : Math.Min(scaleX, scaleY));

				_matrix.SetScale(scale, scale);

				// Center the image
				var redundantXSpace = (float)Width - (scale * (float)bitmapWidth);
				var redundantYSpace = (float)Height - (scale * (float)bitmapHeight);

				redundantXSpace /= (float)2;
				redundantYSpace /= (float)2;

				_matrix.PostTranslate(redundantXSpace, redundantYSpace);

				_originalWidth = Width - 2 * redundantXSpace;
				_originalHeight = Height - 2 * redundantYSpace;

				ImageMatrix = _matrix;
			}
			FixTrans();
		}

		public bool OnTouch(View v, MotionEvent e)
		{
			this.Parent.RequestDisallowInterceptTouchEvent(_saveScale != 1);

			//_scaleDetector.OnTouchEvent(e);
			//_doubleTapDetector.OnTouchEvent(e);

			var currentPoint = new System.Drawing.PointF(e.GetX(), e.GetY());

			switch (e.Action)
			{
				case MotionEventActions.Down:
					_previousPoint = currentPoint;
					_manipulationStart = currentPoint;
					_manipulationMode = ManipulationMode.Drag;
					break;

				case MotionEventActions.Move:
					if (_manipulationMode == ManipulationMode.Drag)
					{
						var deltaX = currentPoint.X - _previousPoint.X;
						var deltaY = currentPoint.Y - _previousPoint.Y;

						var fixTransX = GetFixDragTrans(deltaX, _viewWidth, _originalWidth * _saveScale);
						var fixTransY = GetFixDragTrans(deltaY, _viewHeight, _originalHeight * _saveScale);

						_matrix.PostTranslate(fixTransX, fixTransY);
						FixTrans();

						_previousPoint.X = currentPoint.X;
						_previousPoint.Y = currentPoint.Y;
					}
					break;

				case MotionEventActions.Up:
					_manipulationMode = ManipulationMode.None;

					var diffX = (int)Math.Abs(currentPoint.X - _manipulationStart.X);
					var diffY = (int)Math.Abs(currentPoint.Y - _manipulationStart.Y);

					if (diffX < BindableImageView.DeltaClick && diffY < BindableImageView.DeltaClick)
					{
						PerformClick();
					}

					break;

				case MotionEventActions.PointerUp:
					_manipulationMode = ManipulationMode.None;
					break;
			}

			ImageMatrix = _matrix;
			Invalidate();

			return true;
		}

		public void OnScaleBegin()
		{
			_manipulationMode = ManipulationMode.Zoom;
		}

		public void OnScale(ScaleGestureDetector detector)
		{
			Scale(detector.ScaleFactor, detector.FocusX, detector.FocusY);
		}

		public virtual void QuickScale(float centerX, float centerY)
		{
			Scale(_saveScale > MinZoom ? 0.0f : 2f, centerX, centerY, true);
		}

		private void Scale(float scaleFactor, float focusX, float focusY, bool animate = false)
		{
			animate = false; // animate disabled by default, as it doesn't work perfectly

			var originalScale = _saveScale;
			_saveScale *= scaleFactor;

			if (_saveScale > MaxZoom)
			{
				_saveScale = MaxZoom;
				scaleFactor = MaxZoom / originalScale;
			}
			else if (_saveScale < MinZoom)
			{
				_saveScale = MinZoom;
				scaleFactor = MinZoom / originalScale;
			}

			if (_originalWidth * _saveScale <= _viewWidth || _originalHeight * _saveScale <= _viewHeight)
			{
				focusX = _viewWidth / 2;
				focusY = _viewHeight / 2;
			}

			if (animate)
			{
				//ScaleAnimation animation = new ScaleAnimation(scaleFactor, scaleFactor, focusX, focusY)
				//{
				//	Duration = 300
				//};

				//scale.AnimationEnd += (o, args) =>
				//{
				//	_matrix.PostScale(scaleFactor, scaleFactor, focusX, focusY);
				//	FixTrans();
				//};
				//this.StartAnimation(animation);


				//ObjectAnimator scaleX = ObjectAnimator.OfFloat(this, "scaleX", _saveScale);
				//ObjectAnimator scaleY = ObjectAnimator.OfFloat(this, "scaleY", _saveScale);

				//var animationDuration = 300;

				//scaleX.SetDuration(animationDuration);
				//scaleY.SetDuration(animationDuration);

				//AnimatorSet scale = new AnimatorSet();
				//scale.Play(scaleX).With(scaleY);
				//scale.Start();
			}
			else
			{
				_matrix.PostScale(scaleFactor, scaleFactor, focusX, focusY);
				FixTrans();
			}
		}

		private void FixTrans()
		{
			if (_matrix != null)
			{
				_matrix.GetValues(_m);

				float transX = _m[Matrix.MtransX];
				float transY = _m[Matrix.MtransY];

				float fixTransX = GetFixTrans(transX, _viewWidth, _originalWidth * _saveScale);
				float fixTransY = GetFixTrans(transY, _viewHeight, _originalHeight * _saveScale);

				if (fixTransX != 0 || fixTransY != 0)
				{
					_matrix.PostTranslate(fixTransX, fixTransY);
				}
			}
		}

		private enum ManipulationMode
		{
			None,
			Drag,
			Zoom
		}

		private static float GetFixTrans(float trans, float viewSize, float contentSize)
		{
			var minTrans = contentSize <= viewSize ? 0 : viewSize - contentSize;
			var maxTrans = contentSize <= viewSize ? viewSize - contentSize : 0;

			return trans < minTrans
				? -trans + minTrans
				: (trans > maxTrans ? -trans + maxTrans : 0);
		}

		private static float GetFixDragTrans(float delta, float viewSize, float contentSize)
		{
			return contentSize <= viewSize ? 0 : delta;
		}

		private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
		{
			private BindableImageView _parent;

			public ScaleListener(BindableImageView parent)
			{
				_parent = parent;
			}

			public override bool OnScaleBegin(ScaleGestureDetector detector)
			{
				_parent.OnScaleBegin();
				return true;
			}

			public override bool OnScale(ScaleGestureDetector detector)
			{
				_parent.OnScale(detector);
				return true;
			}
		}

		private class DoubleTapListener : GestureDetector.SimpleOnGestureListener
		{
			private BindableImageView _parent;

			public DoubleTapListener(BindableImageView parent)
			{
				_parent = parent;
			}

			public override bool OnDoubleTap(MotionEvent e)
			{
				_parent.QuickScale(e.GetX(), e.GetY());
				return true;
			}
		}

		#endregion

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
