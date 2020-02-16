#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorePackageUpdate 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool Mandatory
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorePackageUpdate.Mandatory is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package StorePackageUpdate.Package is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StorePackageUpdate.Package.get
		// Forced skipping of method Windows.Services.Store.StorePackageUpdate.Mandatory.get
	}
}
