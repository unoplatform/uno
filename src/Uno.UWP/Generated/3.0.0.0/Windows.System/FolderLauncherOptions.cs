#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FolderLauncherOptions : global::Windows.System.ILauncherViewOptions
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Storage.IStorageItem> ItemsToSelect
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<IStorageItem> FolderLauncherOptions.ItemsToSelect is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ViewManagement.ViewSizePreference DesiredRemainingView
		{
			get
			{
				throw new global::System.NotImplementedException("The member ViewSizePreference FolderLauncherOptions.DesiredRemainingView is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.FolderLauncherOptions", "ViewSizePreference FolderLauncherOptions.DesiredRemainingView");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public FolderLauncherOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.FolderLauncherOptions", "FolderLauncherOptions.FolderLauncherOptions()");
		}
		#endif
		// Forced skipping of method Windows.System.FolderLauncherOptions.FolderLauncherOptions()
		// Forced skipping of method Windows.System.FolderLauncherOptions.ItemsToSelect.get
		// Forced skipping of method Windows.System.FolderLauncherOptions.DesiredRemainingView.get
		// Forced skipping of method Windows.System.FolderLauncherOptions.DesiredRemainingView.set
		// Processing: Windows.System.ILauncherViewOptions
	}
}
