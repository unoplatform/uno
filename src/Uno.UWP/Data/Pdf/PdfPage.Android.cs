#if __ANDROID__
using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Pdf;
using Uno;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Color = Windows.UI.Color;
using Rect = Windows.Foundation.Rect;

namespace Windows.Data.Pdf
{
	public partial class PdfPage : IDisposable
	{
		private readonly PdfRenderer.Page pdfPage;

		internal PdfPage(PdfRenderer.Page pdfPage)
		{
			this.pdfPage = pdfPage ?? throw new ArgumentNullException(nameof(pdfPage));
		}

		public PdfPageDimensions Dimensions { get; } = new PdfPageDimensions();

		public uint Index => (uint)pdfPage.Index;

		[NotImplemented]
		public float PreferredZoom
		{
			get
			{
				throw new NotImplementedException("The member float PdfPage.PreferredZoom is not implemented in Uno.");
			}
		}

		[NotImplemented]
		public PdfPageRotation Rotation
		{
			get
			{
				throw new NotImplementedException("The member PdfPageRotation PdfPage.Rotation is not implemented in Uno.");
			}
		}

		public Size Size => new Size(pdfPage.Width, pdfPage.Height);

		public IAsyncAction RenderToStreamAsync(IRandomAccessStream outputStream)
		{
			if (outputStream is null)
			{
				throw new ArgumentNullException(nameof(outputStream));
			}

			var options = new PdfPageRenderOptions
			{
				DestinationWidth = (uint)pdfPage.Width,
				DestinationHeight = (uint)pdfPage.Height
			};
			return RenderToStreamAsync(outputStream, options);
		}

		public IAsyncAction RenderToStreamAsync(IRandomAccessStream outputStream, PdfPageRenderOptions options)
		{
			#region Validate arguments
			if (outputStream is null)
			{
				throw new ArgumentNullException(nameof(outputStream));
			}
			if (options is null)
			{
				throw new ArgumentNullException(nameof(options));
			}
			if (options.DestinationWidth > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(options), $"PdfPageRenderOptions.DestinationWidth = {options.DestinationWidth}. Must be less than or equal to int.MaxValue");
			}
			if (options.DestinationHeight > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(options), $"PdfPageRenderOptions.DestinationHeight = {options.DestinationHeight}. Must be less than or equal to int.MaxValue");
			}
			#endregion

			return Task.Run(() =>
			{
				const int quality = 100;
				var bitmap = RenderInternal(options);
				bitmap.Compress(Bitmap.CompressFormat.Png, quality, outputStream.AsStream());
				return bitmap;
			}).AsAsyncAction();
		}

		private Bitmap RenderInternal(PdfPageRenderOptions options)
		{
			var destination = new Size(options.DestinationWidth, options.DestinationHeight);

			#region handle 0-width and 0-height
			if (destination.Width == 0 && destination.Height == 0)
			{
				// destination size not set - render with the page original size
				destination = Size;
			}
			else if (destination.Width == 0)
			{
				// destination width not set - calculate it based on height proportion
				var scale = destination.Height / Size.Height;
				destination = new Size(Size.Width * scale, destination.Height);
			}
			else if (destination.Height == 0)
			{
				// destination height not set - calculate it based on width proportion
				var scale = destination.Width / Size.Width;
				destination = new Size(destination.Width, Size.Height * scale);
			}
			#endregion

			#region scale according to DPI
			var dpi = Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;
			destination = new Size(destination.Width * dpi, destination.Height * dpi);
			#endregion

			#region create bitmap
			var bitmap = Bitmap.CreateBitmap((int)destination.Width, (int)destination.Height, Bitmap.Config.Argb8888);
			var destinationRect = new Rect(new Foundation.Point(), destination);
			#endregion

			#region fill with background color
			using (var canvas = new Canvas(bitmap))
			{
				var color = options.BackgroundColor.Equals(default(Color)) ? Colors.White : options.BackgroundColor;
				canvas.DrawRect(destinationRect, new Paint()
				{
					Color = color
				});
			}
			#endregion

			#region render only a portion of the page, defined by PdfPageRenderOptions.SourceRect, if set
			var sourceRect = options.SourceRect;
			Matrix matrix = null;
			if (sourceRect.Width > 0 && sourceRect.Height > 0)
			{
				matrix = new Matrix();
				matrix.SetRectToRect((RectF)sourceRect, (RectF)destinationRect, Matrix.ScaleToFit.Start);
			}
			#endregion

			// Render content
			pdfPage.Render(bitmap, null, matrix, PdfRenderMode.ForDisplay);
			return bitmap;
		}

		public IAsyncAction PreparePageAsync()
		{
			return Task.CompletedTask.AsAsyncAction();
		}

		public void Dispose()
		{
			pdfPage.Close();
			pdfPage.Dispose();
		}
	}
}

#endif
