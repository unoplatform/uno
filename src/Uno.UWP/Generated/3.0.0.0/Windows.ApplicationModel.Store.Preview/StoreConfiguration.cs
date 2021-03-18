#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StoreConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Store.Preview.StoreHardwareManufacturerInfo HardwareManufacturerInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member StoreHardwareManufacturerInfo StoreConfiguration.HardwareManufacturerInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static uint? PurchasePromptingPolicy
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? StoreConfiguration.PurchasePromptingPolicy is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "uint? StoreConfiguration.PurchasePromptingPolicy");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsPinToDesktopSupported()
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.IsPinToDesktopSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsPinToTaskbarSupported()
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.IsPinToTaskbarSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsPinToStartSupported()
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.IsPinToStartSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void PinToDesktop( string appPackageFamilyName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.PinToDesktop(string appPackageFamilyName)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void PinToDesktopForUser( global::Windows.System.User user,  string appPackageFamilyName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.PinToDesktopForUser(User user, string appPackageFamilyName)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetStoreWebAccountId()
		{
			throw new global::System.NotImplementedException("The member string StoreConfiguration.GetStoreWebAccountId() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetStoreWebAccountIdForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member string StoreConfiguration.GetStoreWebAccountIdForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetEnterpriseStoreWebAccountId( string webAccountId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.SetEnterpriseStoreWebAccountId(string webAccountId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetEnterpriseStoreWebAccountIdForUser( global::Windows.System.User user,  string webAccountId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.SetEnterpriseStoreWebAccountIdForUser(User user, string webAccountId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetEnterpriseStoreWebAccountId()
		{
			throw new global::System.NotImplementedException("The member string StoreConfiguration.GetEnterpriseStoreWebAccountId() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetEnterpriseStoreWebAccountIdForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member string StoreConfiguration.GetEnterpriseStoreWebAccountIdForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool ShouldRestrictToEnterpriseStoreOnly()
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.ShouldRestrictToEnterpriseStoreOnly() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool ShouldRestrictToEnterpriseStoreOnlyForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.ShouldRestrictToEnterpriseStoreOnlyForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool HasStoreWebAccount()
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.HasStoreWebAccount() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool HasStoreWebAccountForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.HasStoreWebAccountForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStreamReference> GetStoreLogDataAsync( global::Windows.ApplicationModel.Store.Preview.StoreLogOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStreamReference> StoreConfiguration.GetStoreLogDataAsync(StoreLogOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetStoreWebAccountIdForUser( global::Windows.System.User user,  string webAccountId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.SetStoreWebAccountIdForUser(User user, string webAccountId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsStoreWebAccountIdForUser( global::Windows.System.User user,  string webAccountId)
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.IsStoreWebAccountIdForUser(User user, string webAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static uint? GetPurchasePromptingPolicyForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member uint? StoreConfiguration.GetPurchasePromptingPolicyForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetPurchasePromptingPolicyForUser( global::Windows.System.User user,  uint? value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.SetPurchasePromptingPolicyForUser(User user, uint? value)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.StoreConfiguration.PurchasePromptingPolicy.get
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.StoreConfiguration.PurchasePromptingPolicy.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetSystemConfiguration( string catalogHardwareManufacturerId,  string catalogStoreContentModifierId,  global::System.DateTimeOffset systemConfigurationExpiration,  string catalogHardwareDescriptor)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.SetSystemConfiguration(string catalogHardwareManufacturerId, string catalogStoreContentModifierId, DateTimeOffset systemConfigurationExpiration, string catalogHardwareDescriptor)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetMobileOperatorConfiguration( string mobileOperatorId,  uint appDownloadLimitInMegabytes,  uint updateDownloadLimitInMegabytes)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.SetMobileOperatorConfiguration(string mobileOperatorId, uint appDownloadLimitInMegabytes, uint updateDownloadLimitInMegabytes)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetStoreWebAccountId( string webAccountId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.Preview.StoreConfiguration", "void StoreConfiguration.SetStoreWebAccountId(string webAccountId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsStoreWebAccountId( string webAccountId)
		{
			throw new global::System.NotImplementedException("The member bool StoreConfiguration.IsStoreWebAccountId(string webAccountId) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.Preview.StoreConfiguration.HardwareManufacturerInfo.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Store.Preview.StoreSystemFeature>> FilterUnsupportedSystemFeaturesAsync( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Store.Preview.StoreSystemFeature> systemFeatures)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StoreSystemFeature>> StoreConfiguration.FilterUnsupportedSystemFeaturesAsync(IEnumerable<StoreSystemFeature> systemFeatures) is not implemented in Uno.");
		}
		#endif
	}
}
