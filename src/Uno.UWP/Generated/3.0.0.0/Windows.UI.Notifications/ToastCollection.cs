#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToastCollection 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string LaunchArgs
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ToastCollection.LaunchArgs is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastCollection", "string ToastCollection.LaunchArgs");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Icon
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri ToastCollection.Icon is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastCollection", "Uri ToastCollection.Icon");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ToastCollection.DisplayName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastCollection", "string ToastCollection.DisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ToastCollection.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ToastCollection( string collectionId,  string displayName,  string launchArgs,  global::System.Uri iconUri) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastCollection", "ToastCollection.ToastCollection(string collectionId, string displayName, string launchArgs, Uri iconUri)");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.ToastCollection(string, string, string, System.Uri)
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.Id.get
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.DisplayName.get
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.DisplayName.set
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.LaunchArgs.get
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.LaunchArgs.set
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.Icon.get
		// Forced skipping of method Windows.UI.Notifications.ToastCollection.Icon.set
	}
}
