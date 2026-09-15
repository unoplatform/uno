using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno;
using Uno.UI;
using Uno.Disposables;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Numerics;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media
{
	public partial class ImageBrush : TileBrush
	{
		private readonly SerialDisposable _sourceDisposable = new SerialDisposable();

#pragma warning disable CS0067 // The event 'ImageBrush.ImageFailed' is never used
		public event RoutedEventHandler ImageOpened;
		public event ExceptionRoutedEventHandler ImageFailed;
#pragma warning restore CS0067 // The event 'ImageBrush.ImageFailed' is never used

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
			else if (newValue is SvgImageSource svgImageSource)
			{
				_sourceDisposable.Disposable = svgImageSource.RegisterDisposablePropertyChangedCallback(
					SvgImageSource.UriSourceProperty,
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

#if __CROSSRUNTIME__
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

		internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
		{
			if (_compositionBrush is null)
			{
				_compositionBrush = compositor.CreateSurfaceBrush();
				SynchronizeCompositionBrush();
			}

			return _compositionBrush;
		}

		internal override void SynchronizeCompositionBrush()
		{
			if (_compositionBrush is CompositionSurfaceBrush surfaceBrush && ImageDataCache is { } data)
			{
				surfaceBrush.Stretch = (CompositionStretch)Stretch;
				surfaceBrush.HorizontalAlignmentRatio = GetHorizontalAlignmentRatio(AlignmentX);
				surfaceBrush.VerticalAlignmentRatio = GetVerticalAlignmentRatio(AlignmentY);
				surfaceBrush.Surface = data.CompositionSurface;
				surfaceBrush.RelativeTransform = RelativeTransform?.MatrixCore ?? Matrix3x2.Identity;
			}
		}

		private static float GetHorizontalAlignmentRatio(AlignmentX alignmentX)
		{
			return alignmentX switch
			{
				AlignmentX.Left => 0.0f,
				AlignmentX.Center => 0.5f,
				AlignmentX.Right => 1.0f,
				_ => 0.5f, // this should never happen.
			};
		}

		private static float GetVerticalAlignmentRatio(AlignmentY alignmentY)
		{
			return alignmentY switch
			{
				AlignmentY.Top => 0.0f,
				AlignmentY.Center => 0.5f,
				AlignmentY.Bottom => 1.0f,
				_ => 0.5f, // this should never happen.
			};
		}
#endif
	}
}
