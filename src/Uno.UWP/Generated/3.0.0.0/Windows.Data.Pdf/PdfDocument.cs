#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Pdf
{
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial class PdfDocument
	{
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsPasswordProtected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PdfDocument.IsPasswordProtected is not implemented in Uno.");
			}
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint PageCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint PdfDocument.PageCount is not implemented in Uno.");
			}
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Pdf.PdfPage GetPage( uint pageIndex)
		{
			throw new global::System.NotImplementedException("The member PdfPage PdfDocument.GetPage(uint pageIndex) is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Pdf.PdfDocument.PageCount.get
		// Forced skipping of method Windows.Data.Pdf.PdfDocument.IsPasswordProtected.get
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Data.Pdf.PdfDocument> LoadFromFileAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromFileAsync(IStorageFile file) is not implemented in Uno.");
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Data.Pdf.PdfDocument> LoadFromFileAsync( global::Windows.Storage.IStorageFile file,  string password)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromFileAsync(IStorageFile file, string password) is not implemented in Uno.");
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Data.Pdf.PdfDocument> LoadFromStreamAsync( global::Windows.Storage.Streams.IRandomAccessStream inputStream)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromStreamAsync(IRandomAccessStream inputStream) is not implemented in Uno.");
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Data.Pdf.PdfDocument> LoadFromStreamAsync( global::Windows.Storage.Streams.IRandomAccessStream inputStream,  string password)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PdfDocument> PdfDocument.LoadFromStreamAsync(IRandomAccessStream inputStream, string password) is not implemented in Uno.");
		}
#endif
	}
}
