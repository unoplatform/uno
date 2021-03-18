#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataWriterStoreOperation : global::Windows.Foundation.IAsyncOperation<uint>,global::Windows.Foundation.IAsyncInfo
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception DataWriterStoreOperation.ErrorCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DataWriterStoreOperation.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.AsyncStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AsyncStatus DataWriterStoreOperation.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.AsyncOperationCompletedHandler<uint> Completed
		{
			get
			{
				throw new global::System.NotImplementedException("The member AsyncOperationCompletedHandler<uint> DataWriterStoreOperation.Completed is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriterStoreOperation", "AsyncOperationCompletedHandler<uint> DataWriterStoreOperation.Completed");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.DataWriterStoreOperation.Completed.set
		// Forced skipping of method Windows.Storage.Streams.DataWriterStoreOperation.Completed.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint GetResults()
		{
			throw new global::System.NotImplementedException("The member uint DataWriterStoreOperation.GetResults() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.DataWriterStoreOperation.Id.get
		// Forced skipping of method Windows.Storage.Streams.DataWriterStoreOperation.Status.get
		// Forced skipping of method Windows.Storage.Streams.DataWriterStoreOperation.ErrorCode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriterStoreOperation", "void DataWriterStoreOperation.Cancel()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Streams.DataWriterStoreOperation", "void DataWriterStoreOperation.Close()");
		}
		#endif
		// Processing: Windows.Foundation.IAsyncOperation<uint>
		// Processing: Windows.Foundation.IAsyncInfo
	}
}
