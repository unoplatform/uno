#if __ANDROID__
using System;
using System.Threading.Tasks;
using Android.Graphics.Pdf;
using Uno;

namespace Windows.Data.Pdf
{
	public partial class PdfDocument : IDisposable
	{
		private readonly PdfRenderer pdfRenderer;

		private PdfDocument(PdfRenderer pdfRenderer)
		{
			this.pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
		}

		[NotImplemented]
		public bool IsPasswordProtected => false;

		public uint PageCount => (uint)pdfRenderer.PageCount;

		public PdfPage GetPage(uint pageIndex)
		{
			if (pageIndex >= pdfRenderer.PageCount)
			{
				throw new ArgumentOutOfRangeException(nameof(pageIndex), $"In this document the page index cannot be {pageIndex}. Page count is {pdfRenderer.PageCount}");
			}

			var pdfPage = pdfRenderer.OpenPage((int)pageIndex);
			return new PdfPage(pdfPage);
		}

		public static Foundation.IAsyncOperation<PdfDocument> LoadFromFileAsync(Storage.IStorageFile file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

#pragma warning disable Uno0001 // Uno type or member is not implemented
			var localpath = file.Path;
#pragma warning restore Uno0001 // Uno type or member is not implemented
			var javaFile = new Java.IO.File(localpath);
			var fileDescriptor = Android.OS.ParcelFileDescriptor.Open(javaFile, Android.OS.ParcelFileMode.ReadOnly);
			var pdfRenderer = new PdfRenderer(fileDescriptor);
			var pdfDocument = new PdfDocument(pdfRenderer);

			return Task.FromResult(pdfDocument).AsAsyncOperation();
		}


		[NotImplemented]
		public static Foundation.IAsyncOperation<PdfDocument> LoadFromFileAsync(Storage.IStorageFile file, string password)
		{
			// password protected PDF files are not supported by PdfRenderer in Android
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromFileAsync(IStorageFile file, string password) is not implemented in Uno.");
		}

		[NotImplemented]
		public static Foundation.IAsyncOperation<PdfDocument> LoadFromStreamAsync(Storage.Streams.IRandomAccessStream inputStream)
		{
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromStreamAsync(IRandomAccessStream inputStream) is not implemented in Uno.");
		}


		[NotImplemented]
		public static Foundation.IAsyncOperation<PdfDocument> LoadFromStreamAsync(Storage.Streams.IRandomAccessStream inputStream, string password)
		{
			// password protected PDF files are not supported by PdfRenderer in Android
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromStreamAsync(IRandomAccessStream inputStream, string password) is not implemented in Uno.");
		}

		public void Dispose()
		{
			pdfRenderer.Dispose();
		}
	}
}

#endif
