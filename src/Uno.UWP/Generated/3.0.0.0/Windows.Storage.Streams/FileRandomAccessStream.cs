#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FileRandomAccessStream : global::Windows.Storage.Streams.IRandomAccessStream,global::Windows.Storage.Streams.IOutputStream,global::System.IDisposable,global::Windows.Storage.Streams.IInputStream
	{
		// Skipping already declared property Size
		// Skipping already declared property CanRead
		// Skipping already declared property CanWrite
		// Skipping already declared property Position
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.Size.get
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.Size.set
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.GetInputStreamAt(ulong)
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.GetOutputStreamAt(ulong)
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.Position.get
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.Seek(ulong)
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.CloneStream()
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.CanRead.get
		// Forced skipping of method Windows.Storage.Streams.FileRandomAccessStream.CanWrite.get
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.Dispose()
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.ReadAsync(Windows.Storage.Streams.IBuffer, uint, Windows.Storage.Streams.InputStreamOptions)
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.WriteAsync(Windows.Storage.Streams.IBuffer)
		// Skipping already declared method Windows.Storage.Streams.FileRandomAccessStream.FlushAsync()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync( string filePath,  global::Windows.Storage.FileAccessMode accessMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenAsync(string filePath, FileAccessMode accessMode) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIRandomAccessStream%3E%20FileRandomAccessStream.OpenAsync%28string%20filePath%2C%20FileAccessMode%20accessMode%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync( string filePath,  global::Windows.Storage.FileAccessMode accessMode,  global::Windows.Storage.StorageOpenOptions sharingOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenAsync(string filePath, FileAccessMode accessMode, StorageOpenOptions sharingOptions, FileOpenDisposition openDisposition) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIRandomAccessStream%3E%20FileRandomAccessStream.OpenAsync%28string%20filePath%2C%20FileAccessMode%20accessMode%2C%20StorageOpenOptions%20sharingOptions%2C%20FileOpenDisposition%20openDisposition%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync( string filePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteAsync(string filePath) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageStreamTransaction%3E%20FileRandomAccessStream.OpenTransactedWriteAsync%28string%20filePath%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync( string filePath,  global::Windows.Storage.StorageOpenOptions openOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteAsync(string filePath, StorageOpenOptions openOptions, FileOpenDisposition openDisposition) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageStreamTransaction%3E%20FileRandomAccessStream.OpenTransactedWriteAsync%28string%20filePath%2C%20StorageOpenOptions%20openOptions%2C%20FileOpenDisposition%20openDisposition%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenForUserAsync( global::Windows.System.User user,  string filePath,  global::Windows.Storage.FileAccessMode accessMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenForUserAsync(User user, string filePath, FileAccessMode accessMode) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIRandomAccessStream%3E%20FileRandomAccessStream.OpenForUserAsync%28User%20user%2C%20string%20filePath%2C%20FileAccessMode%20accessMode%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenForUserAsync( global::Windows.System.User user,  string filePath,  global::Windows.Storage.FileAccessMode accessMode,  global::Windows.Storage.StorageOpenOptions sharingOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> FileRandomAccessStream.OpenForUserAsync(User user, string filePath, FileAccessMode accessMode, StorageOpenOptions sharingOptions, FileOpenDisposition openDisposition) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIRandomAccessStream%3E%20FileRandomAccessStream.OpenForUserAsync%28User%20user%2C%20string%20filePath%2C%20FileAccessMode%20accessMode%2C%20StorageOpenOptions%20sharingOptions%2C%20FileOpenDisposition%20openDisposition%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteForUserAsync( global::Windows.System.User user,  string filePath)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteForUserAsync(User user, string filePath) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageStreamTransaction%3E%20FileRandomAccessStream.OpenTransactedWriteForUserAsync%28User%20user%2C%20string%20filePath%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteForUserAsync( global::Windows.System.User user,  string filePath,  global::Windows.Storage.StorageOpenOptions openOptions,  global::Windows.Storage.Streams.FileOpenDisposition openDisposition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> FileRandomAccessStream.OpenTransactedWriteForUserAsync(User user, string filePath, StorageOpenOptions openOptions, FileOpenDisposition openDisposition) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageStreamTransaction%3E%20FileRandomAccessStream.OpenTransactedWriteForUserAsync%28User%20user%2C%20string%20filePath%2C%20StorageOpenOptions%20openOptions%2C%20FileOpenDisposition%20openDisposition%29");
		}
		#endif
		// Processing: Windows.Storage.Streams.IRandomAccessStream
		// Processing: System.IDisposable
		// Processing: Windows.Storage.Streams.IInputStream
		// Processing: Windows.Storage.Streams.IOutputStream
	}
}
