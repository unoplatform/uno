#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileInputStream : global::Windows.Storage.Streams.IInputStream,global::System.IDisposable
	{
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IBuffer, uint> ReadAsync( global::Windows.Storage.Streams.IBuffer buffer,  uint count,  global::Windows.Storage.Streams.InputStreamOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IBuffer, uint> FileInputStream.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) is not implemented in Uno.");
		}
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.FileInputStream", "void FileInputStream.Dispose()");
		}
		#endif
		// Processing: Windows.Storage.Streams.IInputStream
		// Processing: System.IDisposable
	}
}
