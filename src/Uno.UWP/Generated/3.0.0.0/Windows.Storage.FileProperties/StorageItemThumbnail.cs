#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.FileProperties
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageItemThumbnail : global::Windows.Storage.Streams.IRandomAccessStreamWithContentType,global::Windows.Storage.Streams.IContentTypeProvider,global::Windows.Storage.Streams.IRandomAccessStream,global::Windows.Storage.Streams.IOutputStream,global::System.IDisposable,global::Windows.Storage.Streams.IInputStream
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint OriginalHeight
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StorageItemThumbnail.OriginalHeight is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint OriginalWidth
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StorageItemThumbnail.OriginalWidth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ReturnedSmallerCachedSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorageItemThumbnail.ReturnedSmallerCachedSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.FileProperties.ThumbnailType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member ThumbnailType StorageItemThumbnail.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ContentType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageItemThumbnail.ContentType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong StorageItemThumbnail.Size is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.FileProperties.StorageItemThumbnail", "ulong StorageItemThumbnail.Size");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanRead
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorageItemThumbnail.CanRead is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanWrite
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorageItemThumbnail.CanWrite is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong StorageItemThumbnail.Position is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.Size.get
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.Size.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetInputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IInputStream StorageItemThumbnail.GetInputStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream GetOutputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IOutputStream StorageItemThumbnail.GetOutputStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.Position.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Seek( ulong position)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.FileProperties.StorageItemThumbnail", "void StorageItemThumbnail.Seek(ulong position)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStream CloneStream()
		{
			throw new global::System.NotImplementedException("The member IRandomAccessStream StorageItemThumbnail.CloneStream() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.CanRead.get
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.CanWrite.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.FileProperties.StorageItemThumbnail", "void StorageItemThumbnail.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IBuffer, uint> ReadAsync( global::Windows.Storage.Streams.IBuffer buffer,  uint count,  global::Windows.Storage.Streams.InputStreamOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IBuffer, uint> StorageItemThumbnail.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync( global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<uint, uint> StorageItemThumbnail.WriteAsync(IBuffer buffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> FlushAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StorageItemThumbnail.FlushAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.ContentType.get
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.OriginalWidth.get
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.OriginalHeight.get
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.ReturnedSmallerCachedSize.get
		// Forced skipping of method Windows.Storage.FileProperties.StorageItemThumbnail.Type.get
		// Processing: Windows.Storage.Streams.IRandomAccessStreamWithContentType
		// Processing: Windows.Storage.Streams.IRandomAccessStream
		// Processing: System.IDisposable
		// Processing: Windows.Storage.Streams.IInputStream
		// Processing: Windows.Storage.Streams.IOutputStream
		// Processing: Windows.Storage.Streams.IContentTypeProvider
	}
}
