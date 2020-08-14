#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if false || false || NET461 || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataPackageView 
	{
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::System.Collections.Generic.IReadOnlyList<string> AvailableFormats
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> DataPackageView.AvailableFormats is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackagePropertySetView Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackagePropertySetView DataPackageView.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageOperation RequestedOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageOperation DataPackageView.RequestedOperation is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackageView.Properties.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackageView.RequestedOperation.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportOperationCompleted( global::Windows.ApplicationModel.DataTransfer.DataPackageOperation value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackageView", "void DataPackageView.ReportOperationCompleted(DataPackageOperation value)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackageView.AvailableFormats.get
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  bool Contains( string formatId)
		{
			throw new global::System.NotImplementedException("The member bool DataPackageView.Contains(string formatId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<object> GetDataAsync( string formatId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<object> DataPackageView.GetDataAsync(string formatId) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetTextAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> DataPackageView.GetTextAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetTextAsync( string formatId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> DataPackageView.GetTextAsync(string formatId) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Uri> GetUriAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Uri> DataPackageView.GetUriAsync() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetHtmlFormatAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> DataPackageView.GetHtmlFormatAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Storage.Streams.RandomAccessStreamReference>> GetResourceMapAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyDictionary<string, RandomAccessStreamReference>> DataPackageView.GetResourceMapAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetRtfAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> DataPackageView.GetRtfAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.RandomAccessStreamReference> GetBitmapAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RandomAccessStreamReference> DataPackageView.GetBitmapAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.IStorageItem>> GetStorageItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IStorageItem>> DataPackageView.GetStorageItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Uri> GetApplicationLinkAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Uri> DataPackageView.GetApplicationLinkAsync() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Uri> GetWebLinkAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Uri> DataPackageView.GetWebLinkAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.EnterpriseData.ProtectionPolicyEvaluationResult> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ProtectionPolicyEvaluationResult> DataPackageView.RequestAccessAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.EnterpriseData.ProtectionPolicyEvaluationResult> RequestAccessAsync( string enterpriseId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ProtectionPolicyEvaluationResult> DataPackageView.RequestAccessAsync(string enterpriseId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.EnterpriseData.ProtectionPolicyEvaluationResult UnlockAndAssumeEnterpriseIdentity()
		{
			throw new global::System.NotImplementedException("The member ProtectionPolicyEvaluationResult DataPackageView.UnlockAndAssumeEnterpriseIdentity() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetAcceptedFormatId( string formatId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackageView", "void DataPackageView.SetAcceptedFormatId(string formatId)");
		}
		#endif
	}
}
