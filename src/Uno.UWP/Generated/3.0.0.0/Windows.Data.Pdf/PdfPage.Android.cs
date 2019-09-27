#if __ANDROID__
using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Pdf;

namespace Windows.Data.Pdf
{
	public partial class PdfPage : IDisposable
	{
		private PdfRenderer.Page pdfPage;

		internal PdfPage(PdfRenderer.Page pdfPage)
		{
			this.pdfPage = pdfPage ?? throw new ArgumentNullException(nameof(pdfPage));
		}

		public PdfPageDimensions Dimensions
		{
			get
			{
				throw new NotImplementedException("The member PdfPageDimensions PdfPage.Dimensions is not implemented in Uno.");
			}
		}



		public uint Index => (uint)pdfPage.Index;


		public float PreferredZoom
		{
			get
			{
				throw new NotImplementedException("The member float PdfPage.PreferredZoom is not implemented in Uno.");
			}
		}



		public PdfPageRotation Rotation
		{
			get
			{
				throw new NotImplementedException("The member PdfPageRotation PdfPage.Rotation is not implemented in Uno.");
			}
		}



		public Foundation.Size Size => new Foundation.Size(pdfPage.Width, pdfPage.Height);



		public Foundation.IAsyncAction RenderToStreamAsync(Storage.Streams.IRandomAccessStream outputStream)
		{
			var bitmap = RenderImage();

			bitmap.Compress(Bitmap.CompressFormat.Png, 100, outputStream.AsStream());
			return Task.CompletedTask.AsAsyncAction();
		}



		public Foundation.IAsyncAction RenderToStreamAsync(Storage.Streams.IRandomAccessStream outputStream, PdfPageRenderOptions options)
		{
			throw new NotImplementedException("The member IAsyncAction PdfPage.RenderToStreamAsync(IRandomAccessStream outputStream, PdfPageRenderOptions options) is not implemented in Uno.");
		}

		private Bitmap RenderImage()
		{
			var bitmap = Bitmap.CreateBitmap(pdfPage.Width, pdfPage.Height, Bitmap.Config.Argb8888);

			// Fill with default while color first
			using (var canvas = new Canvas(bitmap))
			{
				var paint = new Paint
				{
					Color = Color.White
				};
				canvas.DrawRect(new Rect(0, 0, pdfPage.Width, pdfPage.Height), paint);
			}

			// Render content
			pdfPage.Render(bitmap, null, null, PdfRenderMode.ForDisplay);
			return bitmap;
		}



		public Foundation.IAsyncAction PreparePageAsync()
		{
			throw new NotImplementedException("The member IAsyncAction PdfPage.PreparePageAsync() is not implemented in Uno.");
		}

		// Forced skipping of method Windows.Data.Pdf.PdfPage.Index.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.Size.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.Dimensions.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.Rotation.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.PreferredZoom.get


		public void Dispose()
		{
			pdfPage.Dispose();
		}
		// Processing: System.IDisposable
	}
}

#endif
