#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GameChatOverlay 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.UI.GameChatOverlayPosition DesiredPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member GameChatOverlayPosition GameChatOverlay.DesiredPosition is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.UI.GameChatOverlay", "GameChatOverlayPosition GameChatOverlay.DesiredPosition");
			}
		}
		#endif
		// Forced skipping of method Windows.Gaming.UI.GameChatOverlay.DesiredPosition.get
		// Forced skipping of method Windows.Gaming.UI.GameChatOverlay.DesiredPosition.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddMessage( string sender,  string message,  global::Windows.Gaming.UI.GameChatMessageOrigin origin)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.UI.GameChatOverlay", "void GameChatOverlay.AddMessage(string sender, string message, GameChatMessageOrigin origin)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Gaming.UI.GameChatOverlay GetDefault()
		{
			throw new global::System.NotImplementedException("The member GameChatOverlay GameChatOverlay.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
