#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DownloadOperation : global::Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation,global::Windows.Networking.BackgroundTransfer.IBackgroundTransferOperationPriority
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundTransferCostPolicy CostPolicy
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTransferCostPolicy DownloadOperation.CostPolicy is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "BackgroundTransferCostPolicy DownloadOperation.CostPolicy");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri RequestedUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri DownloadOperation.RequestedUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "Uri DownloadOperation.RequestedUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Method
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DownloadOperation.Method is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Group
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DownloadOperation.Group is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Guid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid DownloadOperation.Guid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundTransferPriority Priority
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTransferPriority DownloadOperation.Priority is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "BackgroundTransferPriority DownloadOperation.Priority");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundDownloadProgress Progress
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundDownloadProgress DownloadOperation.Progress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.IStorageFile ResultFile
		{
			get
			{
				throw new global::System.NotImplementedException("The member IStorageFile DownloadOperation.ResultFile is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.BackgroundTransferGroup TransferGroup
		{
			get
			{
				throw new global::System.NotImplementedException("The member BackgroundTransferGroup DownloadOperation.TransferGroup is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRandomAccessRequired
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DownloadOperation.IsRandomAccessRequired is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "bool DownloadOperation.IsRandomAccessRequired");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.WebErrorStatus? CurrentWebErrorStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebErrorStatus? DownloadOperation.CurrentWebErrorStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Web.WebErrorStatus> RecoverableWebErrorStatuses
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<WebErrorStatus> DownloadOperation.RecoverableWebErrorStatuses is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.ResultFile.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.Progress.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Networking.BackgroundTransfer.DownloadOperation, global::Windows.Networking.BackgroundTransfer.DownloadOperation> StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> DownloadOperation.StartAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Networking.BackgroundTransfer.DownloadOperation, global::Windows.Networking.BackgroundTransfer.DownloadOperation> AttachAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> DownloadOperation.AttachAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Pause()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "void DownloadOperation.Pause()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Resume()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "void DownloadOperation.Resume()");
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.Guid.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.RequestedUri.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.Method.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.Group.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.CostPolicy.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.CostPolicy.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetResultStreamAt( ulong position)
		{
			throw new global::System.NotImplementedException("The member IInputStream DownloadOperation.GetResultStreamAt(ulong position) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.BackgroundTransfer.ResponseInformation GetResponseInformation()
		{
			throw new global::System.NotImplementedException("The member ResponseInformation DownloadOperation.GetResponseInformation() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.Priority.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.Priority.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.TransferGroup.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.IsRandomAccessRequired.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.IsRandomAccessRequired.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStreamReference GetResultRandomAccessStreamReference()
		{
			throw new global::System.NotImplementedException("The member IRandomAccessStreamReference DownloadOperation.GetResultRandomAccessStreamReference() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.BackgroundTransfer.BackgroundTransferFileRange> GetDownloadedRanges()
		{
			throw new global::System.NotImplementedException("The member IList<BackgroundTransferFileRange> DownloadOperation.GetDownloadedRanges() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.RangesDownloaded.add
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.RangesDownloaded.remove
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.RequestedUri.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.RecoverableWebErrorStatuses.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.DownloadOperation.CurrentWebErrorStatus.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void MakeCurrentInTransferGroup()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "void DownloadOperation.MakeCurrentInTransferGroup()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetRequestHeader( string headerName,  string headerValue)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "void DownloadOperation.SetRequestHeader(string headerName, string headerValue)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveRequestHeader( string headerName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "void DownloadOperation.RemoveRequestHeader(string headerName)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.BackgroundTransfer.DownloadOperation, global::Windows.Networking.BackgroundTransfer.BackgroundTransferRangesDownloadedEventArgs> RangesDownloaded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "event TypedEventHandler<DownloadOperation, BackgroundTransferRangesDownloadedEventArgs> DownloadOperation.RangesDownloaded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.DownloadOperation", "event TypedEventHandler<DownloadOperation, BackgroundTransferRangesDownloadedEventArgs> DownloadOperation.RangesDownloaded");
			}
		}
		#endif
		// Processing: Windows.Networking.BackgroundTransfer.IBackgroundTransferOperation
		// Processing: Windows.Networking.BackgroundTransfer.IBackgroundTransferOperationPriority
	}
}
