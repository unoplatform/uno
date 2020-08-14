#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataReaderLoadOperation : global::Windows.Foundation.IAsyncOperation<uint>,global::Windows.Foundation.IAsyncInfo
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception DataReaderLoadOperation.ErrorCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DataReaderLoadOperation.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.AsyncStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AsyncStatus DataReaderLoadOperation.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.AsyncOperationCompletedHandler<uint> Completed
		{
			get
			{
				throw new global::System.NotImplementedException("The member AsyncOperationCompletedHandler<uint> DataReaderLoadOperation.Completed is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataReaderLoadOperation", "AsyncOperationCompletedHandler<uint> DataReaderLoadOperation.Completed");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.DataReaderLoadOperation.Completed.set
		// Forced skipping of method Windows.Storage.Streams.DataReaderLoadOperation.Completed.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint GetResults()
		{
			throw new global::System.NotImplementedException("The member uint DataReaderLoadOperation.GetResults() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.DataReaderLoadOperation.Id.get
		// Forced skipping of method Windows.Storage.Streams.DataReaderLoadOperation.Status.get
		// Forced skipping of method Windows.Storage.Streams.DataReaderLoadOperation.ErrorCode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataReaderLoadOperation", "void DataReaderLoadOperation.Cancel()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataReaderLoadOperation", "void DataReaderLoadOperation.Close()");
		}
		#endif
		// Processing: Windows.Foundation.IAsyncOperation<uint>
		// Processing: Windows.Foundation.IAsyncInfo
	}
}
