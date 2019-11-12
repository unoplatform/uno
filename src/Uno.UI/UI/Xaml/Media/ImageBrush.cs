using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageBrush : Brush
	{
		public event RoutedEventHandler ImageOpened;
		public event ExceptionRoutedEventHandler ImageFailed;

		public AlignmentX AlignmentX
		{
			get { return (AlignmentX)GetValue(AlignmentXProperty); }
			set { this.SetValue(AlignmentXProperty, value); }
		}
		public static readonly DependencyProperty AlignmentXProperty =
			DependencyProperty.Register("AlignmentX", typeof(AlignmentX), typeof(ImageBrush), new PropertyMetadata(AlignmentX.Center));

		public AlignmentY AlignmentY
		{
			get { return (AlignmentY)GetValue(AlignmentYProperty); }
			set { this.SetValue(AlignmentYProperty, value); }
		}
		public static readonly DependencyProperty AlignmentYProperty =
			DependencyProperty.Register("AlignmentY", typeof(AlignmentY), typeof(ImageBrush), new PropertyMetadata(AlignmentY.Center));

		public static readonly DependencyProperty StretchProperty =
		  DependencyProperty.Register("Stretch", typeof(Stretch), typeof(ImageBrush), new PropertyMetadata(Stretch.Fill, null));
		public Stretch Stretch
		{
			get { return (Stretch)this.GetValue(StretchProperty); }
			set { this.SetValue(StretchProperty, value); }
		}

		public static DependencyProperty ImageSourceProperty { get; } =
			DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(ImageBrush), new PropertyMetadata(null, (s, e) =>
			((ImageBrush)s).OnSourceChangedPartial((ImageSource)e.NewValue, (ImageSource)e.OldValue)));

		public ImageSource ImageSource
		{
			get { return (ImageSource)this.GetValue(ImageSourceProperty); }
			set { this.SetValue(ImageSourceProperty, value); }
		}

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue);

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

		private void OnImageOpened()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(ToString() + " Image opened successfully");
			}

			ImageOpened?.Invoke(this, new RoutedEventArgs(this));
		}

		private void OnImageFailed()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(ToString() + " Image failed to open");
			}

			ImageFailed?.Invoke(this, new ExceptionRoutedEventArgs(this, "Image failed to open"));
		}
	}
}
