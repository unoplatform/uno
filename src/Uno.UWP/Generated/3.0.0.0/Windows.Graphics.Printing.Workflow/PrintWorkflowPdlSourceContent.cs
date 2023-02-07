#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowPdlSourceContent 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ContentType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PrintWorkflowPdlSourceContent.ContentType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PrintWorkflowPdlSourceContent.ContentType");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowPdlSourceContent.ContentType.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetInputStream()
		{
			throw new global::System.NotImplementedException("The member IInputStream PrintWorkflowPdlSourceContent.GetInputStream() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IInputStream%20PrintWorkflowPdlSourceContent.GetInputStream%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetContentFileAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> PrintWorkflowPdlSourceContent.GetContentFileAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageFile%3E%20PrintWorkflowPdlSourceContent.GetContentFileAsync%28%29");
		}
		#endif
	}
}
