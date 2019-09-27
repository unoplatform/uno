#if __ANDROID__
using System;
using System.Threading.Tasks;
using Android.Graphics.Pdf;

namespace Windows.Data.Pdf
{
	public partial class PdfDocument : IDisposable
	{
		private PdfDocument(PdfRenderer pdfRenderer)
        {
			this.pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
		}


		public bool IsPasswordProtected => false;

		public uint PageCount => (uint)pdfRenderer.PageCount;

		public PdfPage GetPage(uint pageIndex)
		{
			// todo: check pageindex
			var pdfPage = pdfRenderer.OpenPage((int)pageIndex);
			return new PdfPage(pdfPage);
		}

		public static Foundation.IAsyncOperation<PdfDocument> LoadFromFileAsync(Storage.IStorageFile file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			var localpath = file.Path;
			var javaFile = new Java.IO.File(localpath);
			var fileDescriptor = Android.OS.ParcelFileDescriptor.Open(javaFile, Android.OS.ParcelFileMode.ReadOnly);
			var pdfRenderer = new PdfRenderer(fileDescriptor);
			
			var pdfDocument = new PdfDocument(pdfRenderer);

			return Task.FromResult(pdfDocument).AsAsyncOperation();
		}


		public static Foundation.IAsyncOperation<PdfDocument> LoadFromFileAsync(Storage.IStorageFile file, string password)
		{
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromFileAsync(IStorageFile file, string password) is not implemented in Uno.");
		}

		public static Foundation.IAsyncOperation<PdfDocument> LoadFromStreamAsync(Storage.Streams.IRandomAccessStream inputStream)
		{
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromStreamAsync(IRandomAccessStream inputStream) is not implemented in Uno.");
		}



		public static Foundation.IAsyncOperation<PdfDocument> LoadFromStreamAsync(Storage.Streams.IRandomAccessStream inputStream, string password)
		{
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromStreamAsync(IRandomAccessStream inputStream, string password) is not implemented in Uno.");
		}

		public void Dispose()
		{
			pdfRenderer.Dispose();
		}

		private readonly PdfRenderer pdfRenderer;
	}
}

#endif
