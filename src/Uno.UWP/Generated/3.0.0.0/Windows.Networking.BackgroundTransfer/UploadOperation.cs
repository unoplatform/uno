#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UploadOperation : global::Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation,global::Windows.Networking.BackgroundTransfer.IBackgroundTransferOperationPriority
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundTransferCostPolicy CostPolicy
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTransferCostPolicy UploadOperation.CostPolicy is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.UploadOperation", "BackgroundTransferCostPolicy UploadOperation.CostPolicy");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Group
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UploadOperation.Group is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Guid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid UploadOperation.Guid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Method
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UploadOperation.Method is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri RequestedUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri UploadOperation.RequestedUri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundTransferPriority Priority
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTransferPriority UploadOperation.Priority is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.UploadOperation", "BackgroundTransferPriority UploadOperation.Priority");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundUploadProgress Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundUploadProgress UploadOperation.Progress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.IStorageFile SourceFile
		{
			get
			{
				throw new global::System.NotImplementedException("The member IStorageFile UploadOperation.SourceFile is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundTransferGroup TransferGroup
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTransferGroup UploadOperation.TransferGroup is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.SourceFile.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.Progress.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Networking.BackgroundTransfer.UploadOperation, global::Windows.Networking.BackgroundTransfer.UploadOperation> StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<UploadOperation, UploadOperation> UploadOperation.StartAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Networking.BackgroundTransfer.UploadOperation, global::Windows.Networking.BackgroundTransfer.UploadOperation> AttachAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<UploadOperation, UploadOperation> UploadOperation.AttachAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.Guid.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.RequestedUri.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.Method.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.Group.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.CostPolicy.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.CostPolicy.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetResultStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IInputStream UploadOperation.GetResultStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.ResponseInformation GetResponseInformation()
		{
			throw new global::System.NotImplementedException("The member ResponseInformation UploadOperation.GetResponseInformation() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.Priority.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.Priority.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.UploadOperation.TransferGroup.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void MakeCurrentInTransferGroup()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.UploadOperation", "void UploadOperation.MakeCurrentInTransferGroup()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetRequestHeader( string headerName,  string headerValue)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.UploadOperation", "void UploadOperation.SetRequestHeader(string headerName, string headerValue)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveRequestHeader( string headerName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.UploadOperation", "void UploadOperation.RemoveRequestHeader(string headerName)");
		}
		#endif
		// Processing: Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation
		// Processing: Windows.Networking.BackgroundTransfer.IBackgroundTransferOperationPriority
	}
}
