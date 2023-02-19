#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppExtensions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppExtension 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.AppInfo AppInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppInfo AppExtension.AppInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInfo%20AppExtension.AppInfo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Description
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppExtension.Description is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppExtension.Description");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppExtension.DisplayName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppExtension.DisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppExtension.Id is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppExtension.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Package Package
		{
			get
			{
				throw new global::System.NotImplementedException("The member Package AppExtension.Package is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Package%20AppExtension.Package");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AppUserModelId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppExtension.AppUserModelId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppExtension.AppUserModelId");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtension.Id.get
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtension.DisplayName.get
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtension.Description.get
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtension.Package.get
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtension.AppInfo.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Foundation.Collections.IPropertySet> GetExtensionPropertiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IPropertySet> AppExtension.GetExtensionPropertiesAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIPropertySet%3E%20AppExtension.GetExtensionPropertiesAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetPublicFolderAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> AppExtension.GetPublicFolderAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageFolder%3E%20AppExtension.GetPublicFolderAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppExtensions.AppExtension.AppUserModelId.get
	}
}
