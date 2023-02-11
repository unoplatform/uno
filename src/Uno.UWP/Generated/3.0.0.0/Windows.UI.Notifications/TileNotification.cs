#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TileNotification 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Tag
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TileNotification.Tag is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20TileNotification.Tag");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.TileNotification", "string TileNotification.Tag");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset? ExpirationTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? TileNotification.ExpirationTime is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DateTimeOffset%3F%20TileNotification.ExpirationTime");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.TileNotification", "DateTimeOffset? TileNotification.ExpirationTime");
			}
		}
		#endif
		// Skipping already declared property Content
		// Skipping already declared method Windows.UI.Notifications.TileNotification.TileNotification(Windows.Data.Xml.Dom.XmlDocument)
		// Forced skipping of method Windows.UI.Notifications.TileNotification.TileNotification(Windows.Data.Xml.Dom.XmlDocument)
		// Forced skipping of method Windows.UI.Notifications.TileNotification.Content.get
		// Forced skipping of method Windows.UI.Notifications.TileNotification.ExpirationTime.set
		// Forced skipping of method Windows.UI.Notifications.TileNotification.ExpirationTime.get
		// Forced skipping of method Windows.UI.Notifications.TileNotification.Tag.set
		// Forced skipping of method Windows.UI.Notifications.TileNotification.Tag.get
	}
}
