#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.DataProtection
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataProtectionManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.DataProtection.UserDataStorageItemProtectionStatus> ProtectStorageItemAsync( global::Windows.Storage.IStorageItem storageItem,  global::Windows.Security.DataProtection.UserDataAvailability availability)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataStorageItemProtectionStatus> UserDataProtectionManager.ProtectStorageItemAsync(IStorageItem storageItem, UserDataAvailability availability) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.DataProtection.UserDataStorageItemProtectionInfo> GetStorageItemProtectionInfoAsync( global::Windows.Storage.IStorageItem storageItem)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataStorageItemProtectionInfo> UserDataProtectionManager.GetStorageItemProtectionInfoAsync(IStorageItem storageItem) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> ProtectBufferAsync( global::Windows.Storage.Streams.IBuffer unprotectedBuffer,  global::Windows.Security.DataProtection.UserDataAvailability availability)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> UserDataProtectionManager.ProtectBufferAsync(IBuffer unprotectedBuffer, UserDataAvailability availability) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.DataProtection.UserDataBufferUnprotectResult> UnprotectBufferAsync( global::Windows.Storage.Streams.IBuffer protectedBuffer)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataBufferUnprotectResult> UserDataProtectionManager.UnprotectBufferAsync(IBuffer protectedBuffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsContinuedDataAvailabilityExpected( global::Windows.Security.DataProtection.UserDataAvailability availability)
		{
			throw new global::System.NotImplementedException("The member bool UserDataProtectionManager.IsContinuedDataAvailabilityExpected(UserDataAvailability availability) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Security.DataProtection.UserDataProtectionManager.DataAvailabilityStateChanged.add
		// Forced skipping of method Windows.Security.DataProtection.UserDataProtectionManager.DataAvailabilityStateChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.DataProtection.UserDataProtectionManager TryGetDefault()
		{
			throw new global::System.NotImplementedException("The member UserDataProtectionManager UserDataProtectionManager.TryGetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.DataProtection.UserDataProtectionManager TryGetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member UserDataProtectionManager UserDataProtectionManager.TryGetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Security.DataProtection.UserDataProtectionManager, global::Windows.Security.DataProtection.UserDataAvailabilityStateChangedEventArgs> DataAvailabilityStateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.DataProtection.UserDataProtectionManager", "event TypedEventHandler<UserDataProtectionManager, UserDataAvailabilityStateChangedEventArgs> UserDataProtectionManager.DataAvailabilityStateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.DataProtection.UserDataProtectionManager", "event TypedEventHandler<UserDataProtectionManager, UserDataAvailabilityStateChangedEventArgs> UserDataProtectionManager.DataAvailabilityStateChanged");
			}
		}
		#endif
	}
}
