#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PackageCatalogAddResourcePackageResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception PackageCatalogAddResourcePackageResult.ExtendedError is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20PackageCatalogAddResourcePackageResult.ExtendedError");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsComplete
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PackageCatalogAddResourcePackageResult.IsComplete is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PackageCatalogAddResourcePackageResult.IsComplete");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package PackageCatalogAddResourcePackageResult.Package is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Package%20PackageCatalogAddResourcePackageResult.Package");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.PackageCatalogAddResourcePackageResult.Package.get
		// Forced skipping of method Windows.ApplicationModel.PackageCatalogAddResourcePackageResult.IsComplete.get
		// Forced skipping of method Windows.ApplicationModel.PackageCatalogAddResourcePackageResult.ExtendedError.get
	}
}
