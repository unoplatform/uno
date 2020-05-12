#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TileUpdateManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.TileUpdateManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member TileUpdateManagerForUser TileUpdateManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.TileUpdater CreateTileUpdaterForApplication()
		{
			throw new global::System.NotImplementedException("The member TileUpdater TileUpdateManager.CreateTileUpdaterForApplication() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.TileUpdater CreateTileUpdaterForApplication( string applicationId)
		{
			throw new global::System.NotImplementedException("The member TileUpdater TileUpdateManager.CreateTileUpdaterForApplication(string applicationId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.TileUpdater CreateTileUpdaterForSecondaryTile( string tileId)
		{
			throw new global::System.NotImplementedException("The member TileUpdater TileUpdateManager.CreateTileUpdaterForSecondaryTile(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Data.Xml.Dom.XmlDocument GetTemplateContent( global::Windows.UI.Notifications.TileTemplateType type)
		{
			throw new global::System.NotImplementedException("The member XmlDocument TileUpdateManager.GetTemplateContent(TileTemplateType type) is not implemented in Uno.");
		}
		#endif
	}
}
