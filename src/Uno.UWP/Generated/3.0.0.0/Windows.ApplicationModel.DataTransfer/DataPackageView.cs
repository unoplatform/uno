#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataPackageView 
	{
		// Skipping already declared property AvailableFormats
		// Skipping already declared property Properties
		// Skipping already declared property RequestedOperation
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackageView.Properties.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackageView.RequestedOperation.get
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.ReportOperationCompleted(Windows.ApplicationModel.DataTransfer.DataPackageOperation)
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackageView.AvailableFormats.get
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.Contains(string)
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetDataAsync(string)
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetTextAsync()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetTextAsync( string formatId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> DataPackageView.GetTextAsync(string formatId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cstring%3E%20DataPackageView.GetTextAsync%28string%20formatId%29");
		}
		#endif
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetUriAsync()
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetHtmlFormatAsync()
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetResourceMapAsync()
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetRtfAsync()
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetBitmapAsync()
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetStorageItemsAsync()
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetApplicationLinkAsync()
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.GetWebLinkAsync()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.EnterpriseData.ProtectionPolicyEvaluationResult> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ProtectionPolicyEvaluationResult> DataPackageView.RequestAccessAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CProtectionPolicyEvaluationResult%3E%20DataPackageView.RequestAccessAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.EnterpriseData.ProtectionPolicyEvaluationResult> RequestAccessAsync( string enterpriseId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ProtectionPolicyEvaluationResult> DataPackageView.RequestAccessAsync(string enterpriseId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CProtectionPolicyEvaluationResult%3E%20DataPackageView.RequestAccessAsync%28string%20enterpriseId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.EnterpriseData.ProtectionPolicyEvaluationResult UnlockAndAssumeEnterpriseIdentity()
		{
			throw new global::System.NotImplementedException("The member ProtectionPolicyEvaluationResult DataPackageView.UnlockAndAssumeEnterpriseIdentity() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ProtectionPolicyEvaluationResult%20DataPackageView.UnlockAndAssumeEnterpriseIdentity%28%29");
		}
		#endif
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataPackageView.SetAcceptedFormatId(string)
	}
}
