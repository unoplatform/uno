#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ESimProfileMetadata 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ESimProfileMetadata.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsConfirmationCodeRequired
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ESimProfileMetadata.IsConfirmationCodeRequired is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ESimProfilePolicy Policy
		{
			get
			{
				throw new global::System.NotImplementedException("The member ESimProfilePolicy ESimProfileMetadata.Policy is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStreamReference ProviderIcon
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRandomAccessStreamReference ESimProfileMetadata.ProviderIcon is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ProviderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ESimProfileMetadata.ProviderId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ProviderName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ESimProfileMetadata.ProviderName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.ESimProfileMetadataState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member ESimProfileMetadataState ESimProfileMetadata.State is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.IsConfirmationCodeRequired.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.Policy.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.Id.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.ProviderIcon.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.ProviderId.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.ProviderName.get
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.State.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.ESimOperationResult> DenyInstallAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ESimOperationResult> ESimProfileMetadata.DenyInstallAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Networking.NetworkOperators.ESimOperationResult, global::Windows.Networking.NetworkOperators.ESimProfileInstallProgress> ConfirmInstallAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<ESimOperationResult, ESimProfileInstallProgress> ESimProfileMetadata.ConfirmInstallAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Networking.NetworkOperators.ESimOperationResult, global::Windows.Networking.NetworkOperators.ESimProfileInstallProgress> ConfirmInstallAsync( string confirmationCode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<ESimOperationResult, ESimProfileInstallProgress> ESimProfileMetadata.ConfirmInstallAsync(string confirmationCode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.ESimOperationResult> PostponeInstallAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ESimOperationResult> ESimProfileMetadata.PostponeInstallAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.StateChanged.add
		// Forced skipping of method Windows.Networking.NetworkOperators.ESimProfileMetadata.StateChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.NetworkOperators.ESimProfileMetadata, object> StateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.ESimProfileMetadata", "event TypedEventHandler<ESimProfileMetadata, object> ESimProfileMetadata.StateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.ESimProfileMetadata", "event TypedEventHandler<ESimProfileMetadata, object> ESimProfileMetadata.StateChanged");
			}
		}
		#endif
	}
}
