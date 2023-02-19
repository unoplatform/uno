#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceThumbnail : global::Windows.Storage.Streams.IRandomAccessStreamWithContentType,global::Windows.Storage.Streams.IContentTypeProvider,global::Windows.Storage.Streams.IRandomAccessStream,global::Windows.Storage.Streams.IOutputStream,global::System.IDisposable,global::Windows.Storage.Streams.IInputStream
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ContentType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DeviceThumbnail.ContentType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20DeviceThumbnail.ContentType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong DeviceThumbnail.Size is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20DeviceThumbnail.Size");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceThumbnail", "ulong DeviceThumbnail.Size");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanRead
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DeviceThumbnail.CanRead is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20DeviceThumbnail.CanRead");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanWrite
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DeviceThumbnail.CanWrite is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20DeviceThumbnail.CanWrite");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong DeviceThumbnail.Position is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20DeviceThumbnail.Position");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceThumbnail.Size.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceThumbnail.Size.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetInputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IInputStream DeviceThumbnail.GetInputStreamAt(ulong position) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IInputStream%20DeviceThumbnail.GetInputStreamAt%28ulong%20position%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream GetOutputStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IOutputStream DeviceThumbnail.GetOutputStreamAt(ulong position) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IOutputStream%20DeviceThumbnail.GetOutputStreamAt%28ulong%20position%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceThumbnail.Position.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Seek( ulong position)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceThumbnail", "void DeviceThumbnail.Seek(ulong position)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStream CloneStream()
		{
			throw new global::System.NotImplementedException("The member IRandomAccessStream DeviceThumbnail.CloneStream() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IRandomAccessStream%20DeviceThumbnail.CloneStream%28%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceThumbnail.CanRead.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceThumbnail.CanWrite.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceThumbnail", "void DeviceThumbnail.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IBuffer, uint> ReadAsync( global::Windows.Storage.Streams.IBuffer buffer,  uint count,  global::Windows.Storage.Streams.InputStreamOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IBuffer, uint> DeviceThumbnail.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperationWithProgress%3CIBuffer%2C%20uint%3E%20DeviceThumbnail.ReadAsync%28IBuffer%20buffer%2C%20uint%20count%2C%20InputStreamOptions%20options%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync( global::Windows.Storage.Streams.IBuffer buffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<uint, uint> DeviceThumbnail.WriteAsync(IBuffer buffer) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperationWithProgress%3Cuint%2C%20uint%3E%20DeviceThumbnail.WriteAsync%28IBuffer%20buffer%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> FlushAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> DeviceThumbnail.FlushAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20DeviceThumbnail.FlushAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceThumbnail.ContentType.get
		// Processing: Windows.Storage.Streams.IRandomAccessStreamWithContentType
		// Processing: Windows.Storage.Streams.IRandomAccessStream
		// Processing: System.IDisposable
		// Processing: Windows.Storage.Streams.IInputStream
		// Processing: Windows.Storage.Streams.IOutputStream
		// Processing: Windows.Storage.Streams.IContentTypeProvider
	}
}
