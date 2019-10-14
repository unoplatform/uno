#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Pdf
{
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial class PdfPage : global::System.IDisposable
	{
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Pdf.PdfPageDimensions Dimensions
		{
			get
			{
				throw new global::System.NotImplementedException("The member PdfPageDimensions PdfPage.Dimensions is not implemented in Uno.");
			}
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint Index
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint PdfPage.Index is not implemented in Uno.");
			}
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float PreferredZoom
		{
			get
			{
				throw new global::System.NotImplementedException("The member float PdfPage.PreferredZoom is not implemented in Uno.");
			}
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Data.Pdf.PdfPageRotation Rotation
		{
			get
			{
				throw new global::System.NotImplementedException("The member PdfPageRotation PdfPage.Rotation is not implemented in Uno.");
			}
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size PdfPage.Size is not implemented in Uno.");
			}
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction RenderToStreamAsync( global::Windows.Storage.Streams.IRandomAccessStream outputStream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PdfPage.RenderToStreamAsync(IRandomAccessStream outputStream) is not implemented in Uno.");
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction RenderToStreamAsync( global::Windows.Storage.Streams.IRandomAccessStream outputStream,  global::Windows.Data.Pdf.PdfPageRenderOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PdfPage.RenderToStreamAsync(IRandomAccessStream outputStream, PdfPageRenderOptions options) is not implemented in Uno.");
		}
#endif
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction PreparePageAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PdfPage.PreparePageAsync() is not implemented in Uno.");
		}
#endif
		// Forced skipping of method Windows.Data.Pdf.PdfPage.Index.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.Size.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.Dimensions.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.Rotation.get
		// Forced skipping of method Windows.Data.Pdf.PdfPage.PreferredZoom.get
#if false || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Pdf.PdfPage", "void PdfPage.Dispose()");
		}
#endif
		// Processing: System.IDisposable
	}
}
