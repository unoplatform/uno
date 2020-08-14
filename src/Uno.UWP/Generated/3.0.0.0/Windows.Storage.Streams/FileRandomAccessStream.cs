#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileRandomAccessStream : global::Windows.Storage.Streams.IRandomAccessStream,global::Windows.Storage.Streams.IOutputStream,global::System.IDisposable,global::Windows.Storage.Streams.IInputStream
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong FileRandomAccessStream.Size is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.FileRandomAccessStream", "ulong FileRandomAccessStream.Size");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanRead
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FileRandomAccessStream.CanRead is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanWrite
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FileRandomAccessStream.CanWrite is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong FileRandomAccessStream.Position is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.Size.get
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.Size.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetInputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IInputStream FileRandomAccessStream.GetInputStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream GetOutputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IOutputStream FileRandomAccessStream.GetOutputStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.Position.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Seek( ulong position)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.FileRandomAccessStream", "void FileRandomAccessStream.Seek(ulong position)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStream CloneStream()
		{
			throw new global::System.NotImplementedException("The member IRandomAccessStream FileRandomAccessStream.CloneStream() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.CanRead.get
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.CanWrite.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.FileRandomAccessStream", "void FileRandomAccessStream.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IBuffer, uint> ReadAsync( global::Windows.Storage.Streams.IBuffer buffer,  uint count,  global::Windows.Storage.Streams.InputStreamOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IBuffer, uint> FileRandomAccessStream.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync( global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<uint, uint> FileRandomAccessStream.WriteAsync(IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> FlushAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> FileRandomAccessStream.FlushAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync( string filePath,  global::Windows.Storage.FileAccessMode accessMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenAsync(string filePath, FileAccessMode accessMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync( string filePath,  global::Windows.Storage.FileAccessMode accessMode,  global::Windows.Storage.StorageOpenOptions sharingOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenAsync(string filePath, FileAccessMode accessMode, StorageOpenOptions sharingOptions, FileOpenDisposition openDisposition) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync( string filePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteAsync(string filePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync( string filePath,  global::Windows.Storage.StorageOpenOptions openOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteAsync(string filePath, StorageOpenOptions openOptions, FileOpenDisposition openDisposition) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenForUserAsync( global::Windows.System.User user,  string filePath,  global::Windows.Storage.FileAccessMode accessMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenForUserAsync(User user, string filePath, FileAccessMode accessMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenForUserAsync( global::Windows.System.User user,  string filePath,  global::Windows.Storage.FileAccessMode accessMode,  global::Windows.Storage.StorageOpenOptions sharingOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenForUserAsync(User user, string filePath, FileAccessMode accessMode, StorageOpenOptions sharingOptions, FileOpenDisposition openDisposition) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteForUserAsync( global::Windows.System.User user,  string filePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteForUserAsync(User user, string filePath) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteForUserAsync( global::Windows.System.User user,  string filePath,  global::Windows.Storage.StorageOpenOptions openOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteForUserAsync(User user, string filePath, StorageOpenOptions openOptions, FileOpenDisposition openDisposition) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Storage.Streams.IRandomAccessStream
		// Processing: System.IDisposable
		// Processing: Windows.Storage.Streams.IInputStream
		// Processing: Windows.Storage.Streams.IOutputStream
	}
}
