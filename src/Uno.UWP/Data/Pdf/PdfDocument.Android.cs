using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics.Pdf;
using Android.OS;
using Windows.Foundation;

namespace Windows.Data.Pdf;

public partial class PdfDocument : IDisposable
{
	private readonly PdfRenderer _pdfRenderer;

	private PdfDocument(PdfRenderer pdfRenderer)
	{
		this._pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
	}

	public bool IsPasswordProtected => false;

	public uint PageCount => (uint)_pdfRenderer.PageCount;

	public PdfPage GetPage(uint pageIndex)
	{
		if (pageIndex >= _pdfRenderer.PageCount)
		{
			throw new ArgumentOutOfRangeException(nameof(pageIndex), $"In this document the page index cannot be {pageIndex}. Page count is {_pdfRenderer.PageCount}");
		}

		var pdfPage = _pdfRenderer.OpenPage((int)pageIndex);
		return new PdfPage(pdfPage!);
	}

	public static IAsyncOperation<PdfDocument> LoadFromFileAsync(Storage.IStorageFile file)
	{
		return LoadFromFileAsync(file, null);
	}

	public static IAsyncOperation<PdfDocument> LoadFromFileAsync(Storage.IStorageFile file, string? password)
	{
		if (!string.IsNullOrEmpty(password))
		{
			// password protected PDF files are not supported by PdfRenderer in Android
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromFileAsync(IStorageFile file, string password) is not implemented in Uno.");
		}

		if (file is null)
		{
			throw new ArgumentNullException(nameof(file));
		}

		var localpath = file.Path;
		var fileDescriptor = localpath.StartsWith('/')
			? ParcelFileDescriptor.Open(new Java.IO.File(localpath), ParcelFileMode.ReadOnly)
			: Android.App.Application.Context.ContentResolver!.OpenFileDescriptor(Android.Net.Uri.Parse(localpath)!, "r");
		var pdfRenderer = new PdfRenderer(fileDescriptor!);
		var pdfDocument = new PdfDocument(pdfRenderer);

		return Task.FromResult(pdfDocument).AsAsyncOperation();
	}

	public static IAsyncOperation<PdfDocument> LoadFromStreamAsync(Storage.Streams.IRandomAccessStream inputStream)
	{
		return LoadFromStreamAsync(inputStream, default);
	}

	public static IAsyncOperation<PdfDocument> LoadFromStreamAsync(Storage.Streams.IRandomAccessStream inputStream, string? password)
	{
		if (!string.IsNullOrEmpty(password))
		{
			// password protected PDF files are not supported by PdfRenderer in Android
			throw new NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromStreamAsync(IRandomAccessStream inputStream, string password) is not implemented in Uno.");
		}
		return AsyncOperation.FromTask(async ct =>
		{
			var parcel = await GetParcelFileDescriptorFromStreamAsync(inputStream.AsStream(), ct);
			var pdfRenderer = new PdfRenderer(parcel);
			var pdfDocument = new PdfDocument(pdfRenderer);
			return pdfDocument;
		});
	}

	public void Dispose()
	{
		_pdfRenderer.Dispose();
	}

	private static Task<ParcelFileDescriptor> GetParcelFileDescriptorFromStreamAsync(Stream input, CancellationToken token) =>
		Task.Run(() =>
			{
				/* PdfRenderer https://developer.android.com/reference/android/graphics/pdf/PdfRenderer#PdfRenderer(android.os.ParcelFileDescriptor)
				 * PdfRenderer needs a seek-able ParcelFileDescriptor,
				 * to get it from a stream (without using hacks),
				 * you need to copy the stream to a temporary file.
				*/
				var fileName = Path.GetTempFileName();
				var file = new Java.IO.File(fileName);
				file.DeleteOnExit();
				using var outputStream = new Java.IO.FileOutputStream(file);

				byte[] buf = new byte[4096];
				var len = 0;
				while (!token.IsCancellationRequested && (len = input.Read(buf, 0, buf.Length)) > 0)
				{
					outputStream.Write(buf, 0, len);
				}
				outputStream.Flush();
				outputStream.Close();
				return ParcelFileDescriptor.Open(file, ParcelFileMode.ReadOnly)!;
			}, token);
}
