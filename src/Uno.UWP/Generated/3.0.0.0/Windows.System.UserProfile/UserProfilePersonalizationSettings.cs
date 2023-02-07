#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.UserProfile
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserProfilePersonalizationSettings 
	{
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.UserProfile.UserProfilePersonalizationSettings Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserProfilePersonalizationSettings UserProfilePersonalizationSettings.Current is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserProfilePersonalizationSettings%20UserProfilePersonalizationSettings.Current");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetLockScreenImageAsync( global::Windows.Storage.StorageFile imageFile)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserProfilePersonalizationSettings.TrySetLockScreenImageAsync(StorageFile imageFile) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20UserProfilePersonalizationSettings.TrySetLockScreenImageAsync%28StorageFile%20imageFile%29");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySetWallpaperImageAsync( global::Windows.Storage.StorageFile imageFile)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserProfilePersonalizationSettings.TrySetWallpaperImageAsync(StorageFile imageFile) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20UserProfilePersonalizationSettings.TrySetWallpaperImageAsync%28StorageFile%20imageFile%29");
		}
		#endif
		// Forced skipping of method Windows.System.UserProfile.UserProfilePersonalizationSettings.Current.get
		// Skipping already declared method Windows.System.UserProfile.UserProfilePersonalizationSettings.IsSupported()
	}
}
