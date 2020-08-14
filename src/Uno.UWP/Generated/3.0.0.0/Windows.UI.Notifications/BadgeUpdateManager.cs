#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BadgeUpdateManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.BadgeUpdateManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member BadgeUpdateManagerForUser BadgeUpdateManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.BadgeUpdater CreateBadgeUpdaterForApplication()
		{
			throw new global::System.NotImplementedException("The member BadgeUpdater BadgeUpdateManager.CreateBadgeUpdaterForApplication() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.BadgeUpdater CreateBadgeUpdaterForApplication( string applicationId)
		{
			throw new global::System.NotImplementedException("The member BadgeUpdater BadgeUpdateManager.CreateBadgeUpdaterForApplication(string applicationId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.BadgeUpdater CreateBadgeUpdaterForSecondaryTile( string tileId)
		{
			throw new global::System.NotImplementedException("The member BadgeUpdater BadgeUpdateManager.CreateBadgeUpdaterForSecondaryTile(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Data.Xml.Dom.XmlDocument GetTemplateContent( global::Windows.UI.Notifications.BadgeTemplateType type)
		{
			throw new global::System.NotImplementedException("The member XmlDocument BadgeUpdateManager.GetTemplateContent(BadgeTemplateType type) is not implemented in Uno.");
		}
		#endif
	}
}
