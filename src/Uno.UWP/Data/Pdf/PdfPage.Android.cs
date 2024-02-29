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

namespace Windows.Data.Pdf;

public partial class PdfPage : IDisposable
{
	private readonly PdfRenderer.Page _pdfPage;
	// It is an empirical value found by rendering through RenderTargetBitmap
	// in png and comparing the dimensions of the images thus obtained.
	private const float pointToPixelFactor = 96.00000000f / 72.00000000f;

	internal PdfPage(PdfRenderer.Page pdfPage)
	{
		_pdfPage = pdfPage ?? throw new ArgumentNullException(nameof(pdfPage));
	}

	[NotImplemented("__ANDROID__")]
	public PdfPageDimensions Dimensions { get; } = new PdfPageDimensions();

	public uint Index => (uint)_pdfPage.Index;

	public Size Size => new Size(Math.Ceiling(_pdfPage.Width * pointToPixelFactor) + double.Epsilon, Math.Ceiling(_pdfPage.Height * pointToPixelFactor) + double.Epsilon);

	public IAsyncAction RenderToStreamAsync(IRandomAccessStream outputStream)
	{
		if (outputStream is null)
		{
			throw new ArgumentNullException(nameof(outputStream));
		}
		var size = Size;
		var options = new PdfPageRenderOptions
		{
			DestinationWidth = (uint)size.Width,
			DestinationHeight = (uint)size.Height
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
			bitmap.Compress(Bitmap.CompressFormat.Png!, quality, outputStream.AsStream());
			bitmap.Dispose();
		}).AsAsyncAction();
	}

	private Bitmap RenderInternal(PdfPageRenderOptions options)
	{
		var destination = new Size(options.DestinationWidth, options.DestinationHeight);

		var sourceRect = options.SourceRect;
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
		destination = new Size(destination.Width, destination.Height);
		#endregion

		#region create bitmap
		var bitmap = Bitmap.CreateBitmap((int)destination.Width, (int)destination.Height, Bitmap.Config.Argb8888!)!;
		RectF destinationRect = new Rect(new Foundation.Point(), destination);
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

		// Render content
		_pdfPage.Render(bitmap, null, null, PdfRenderMode.ForDisplay);

		if (sourceRect.Width > 0 && sourceRect.Height > 0)
		{
			using var oldbitmap = bitmap;
			bitmap = Bitmap.CreateBitmap(oldbitmap, (int)sourceRect.X, (int)sourceRect.Y, (int)sourceRect.Width, (int)sourceRect.Height, null, false)!;
		}

		return bitmap;
	}

	public IAsyncAction PreparePageAsync() =>
		Task.CompletedTask.AsAsyncAction();

	public void Dispose()
	{
		_pdfPage.Close();
		_pdfPage.Dispose();
	}
}
