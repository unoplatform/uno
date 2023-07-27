using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno;
using Uno.UI;
using Uno.Disposables;
using Windows.UI.Xaml.Media.Imaging;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageBrush : Brush
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();

#pragma warning disable CS0067 // The event 'ImageBrush.ImageFailed' is never used
		public event RoutedEventHandler ImageOpened;
		public event ExceptionRoutedEventHandler ImageFailed;
#pragma warning restore CS0067 // The event 'ImageBrush.ImageFailed' is never used

		#region AlignmentX DP
		public static DependencyProperty AlignmentXProperty { get; } =
			DependencyProperty.Register("AlignmentX", typeof(AlignmentX), typeof(ImageBrush), new FrameworkPropertyMetadata(AlignmentX.Center));

#if __WASM__
		[NotImplemented]
#endif
		public AlignmentX AlignmentX
		{
			get => (AlignmentX)GetValue(AlignmentXProperty);
			set => this.SetValue(AlignmentXProperty, value);
		}
		#endregion

		#region AlignmentY DP
		public static DependencyProperty AlignmentYProperty { get; } =
			DependencyProperty.Register("AlignmentY", typeof(AlignmentY), typeof(ImageBrush), new FrameworkPropertyMetadata(AlignmentY.Center));

#if __WASM__
		[NotImplemented]
#endif
		public AlignmentY AlignmentY
		{
			get => (AlignmentY)GetValue(AlignmentYProperty);
			set => this.SetValue(AlignmentYProperty, value);
		}
		#endregion

		#region Stretch DP
		public static DependencyProperty StretchProperty { get; } =
		  DependencyProperty.Register("Stretch", typeof(Stretch), typeof(ImageBrush), new FrameworkPropertyMetadata(defaultValue: Stretch.Fill, propertyChangedCallback: null));

#if __WASM__
		[NotImplemented]
#endif
		public Stretch Stretch
		{
			get => (Stretch)this.GetValue(StretchProperty);
			set => this.SetValue(StretchProperty, value);
		}
		#endregion

		#region ImageSource DP
		public static DependencyProperty ImageSourceProperty { get; } =
			DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(ImageBrush), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) =>
			((ImageBrush)s).OnSourceChanged((ImageSource)e.NewValue, (ImageSource)e.OldValue)));

		public ImageSource ImageSource
		{
			get => (ImageSource)this.GetValue(ImageSourceProperty);
			set => this.SetValue(ImageSourceProperty, value);
		}

		private void OnSourceChanged(ImageSource newValue, ImageSource oldValue)
		{
			if (newValue is BitmapImage bitmapImage)
			{
				_sourceDisposable.Disposable = bitmapImage.RegisterDisposablePropertyChangedCallback(
					BitmapImage.UriSourceProperty,
					(_, _) => OnSourceChangedPartial(newValue, null)
				);
			}
			else
			{
				_sourceDisposable.Disposable = null;
			}

			OnSourceChangedPartial(newValue, oldValue);
		}

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue);
		#endregion

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == ImageSourceProperty)
			{
				OnImageSourceChanged(this, args);
			}
		}

		private static void OnImageSourceChanged(ImageBrush brush, DependencyPropertyChangedEventArgs args)
		{
			if (args.OldValue is ImageSource oldSource)
			{
				oldSource.Invalidated -= brush.OnInvalidateRender;
			}

			if (args.NewValue is ImageSource newSource)
			{
				newSource.Invalidated += brush.OnInvalidateRender;
			}
		}

		internal Rect GetArrangedImageRect(Size sourceSize, Rect targetRect)
		{
			var size = GetArrangedImageSize(sourceSize, targetRect.Size);
			var location = GetArrangedImageLocation(size, targetRect.Size);

			location.X += targetRect.X;
			location.Y += targetRect.Y;

			return new Rect(location, size);
		}

		private Size GetArrangedImageSize(Size sourceSize, Size targetSize)
		{
			var sourceAspectRatio = sourceSize.AspectRatio();
			var targetAspectRatio = targetSize.AspectRatio();

			switch (Stretch)
			{
				default:
				case Stretch.None:
					return sourceSize;
				case Stretch.Fill:
					return targetSize;
				case Stretch.Uniform:
					return targetAspectRatio > sourceAspectRatio
						? new Size(sourceSize.Width * targetSize.Height / sourceSize.Height, targetSize.Height)
						: new Size(targetSize.Width, sourceSize.Height * targetSize.Width / sourceSize.Width);
				case Stretch.UniformToFill:
					return targetAspectRatio < sourceAspectRatio
						? new Size(sourceSize.Width * targetSize.Height / sourceSize.Height, targetSize.Height)
						: new Size(targetSize.Width, sourceSize.Height * targetSize.Width / sourceSize.Width);
			}
		}

		private Point GetArrangedImageLocation(Size finalSize, Size targetSize)
		{
			var location = new Point(
				targetSize.Width - finalSize.Width,
				targetSize.Height - finalSize.Height
			);

			switch (AlignmentX)
			{
				default:
				case AlignmentX.Left:
					location.X *= 0;
					break;
				case AlignmentX.Center:
					location.X *= 0.5;
					break;
				case AlignmentX.Right:
					location.X *= 1;
					break;
			}

			switch (AlignmentY)
			{
				default:
				case AlignmentY.Top:
					location.Y *= 0;
					break;
				case AlignmentY.Center:
					location.Y *= 0.5f;
					break;
				case AlignmentY.Bottom:
					location.Y *= 1;
					break;
			}

			return location;
		}

#if __ANDROID__ || __IOS__ || __MACOS__ || __CROSSRUNTIME__
		private void OnImageOpened()
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug(ToString() + " Image opened successfully");
			}

			ImageOpened?.Invoke(this, new RoutedEventArgs(this));
		}

		private void OnImageFailed()
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug(ToString() + " Image failed to open");
			}

			ImageFailed?.Invoke(this, new ExceptionRoutedEventArgs(this, "Image failed to open"));
		}
#endif
	}
}
