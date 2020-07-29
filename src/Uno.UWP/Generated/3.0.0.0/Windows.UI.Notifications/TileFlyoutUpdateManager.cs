#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TileFlyoutUpdateManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.TileFlyoutUpdater CreateTileFlyoutUpdaterForApplication()
		{
			throw new global::System.NotImplementedException("The member TileFlyoutUpdater TileFlyoutUpdateManager.CreateTileFlyoutUpdaterForApplication() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.TileFlyoutUpdater CreateTileFlyoutUpdaterForApplication( string applicationId)
		{
			throw new global::System.NotImplementedException("The member TileFlyoutUpdater TileFlyoutUpdateManager.CreateTileFlyoutUpdaterForApplication(string applicationId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.TileFlyoutUpdater CreateTileFlyoutUpdaterForSecondaryTile( string tileId)
		{
			throw new global::System.NotImplementedException("The member TileFlyoutUpdater TileFlyoutUpdateManager.CreateTileFlyoutUpdaterForSecondaryTile(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Data.Xml.Dom.XmlDocument GetTemplateContent( global::Windows.UI.Notifications.TileFlyoutTemplateType type)
		{
			throw new global::System.NotImplementedException("The member XmlDocument TileFlyoutUpdateManager.GetTemplateContent(TileFlyoutTemplateType type) is not implemented in Uno.");
		}
		#endif
	}
}
