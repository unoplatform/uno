#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorePackageLicense : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsValid
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorePackageLicense.IsValid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package StorePackageLicense.Package is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StorePackageLicense.LicenseLost.add
		// Forced skipping of method Windows.Services.Store.StorePackageLicense.LicenseLost.remove
		// Forced skipping of method Windows.Services.Store.StorePackageLicense.Package.get
		// Forced skipping of method Windows.Services.Store.StorePackageLicense.IsValid.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ReleaseLicense()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StorePackageLicense", "void StorePackageLicense.ReleaseLicense()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StorePackageLicense", "void StorePackageLicense.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Services.Store.StorePackageLicense, object> LicenseLost
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StorePackageLicense", "event TypedEventHandler<StorePackageLicense, object> StorePackageLicense.LicenseLost");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StorePackageLicense", "event TypedEventHandler<StorePackageLicense, object> StorePackageLicense.LicenseLost");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
