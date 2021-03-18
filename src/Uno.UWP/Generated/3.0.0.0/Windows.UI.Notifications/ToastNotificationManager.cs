#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToastNotificationManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.ToastNotificationHistory History
		{
			get
			{
				throw new global::System.NotImplementedException("The member ToastNotificationHistory ToastNotificationManager.History is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.ToastNotificationManagerForUser GetDefault()
		{
			throw new global::System.NotImplementedException("The member ToastNotificationManagerForUser ToastNotificationManager.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.ToastNotificationManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member ToastNotificationManagerForUser ToastNotificationManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ConfigureNotificationMirroring( global::Windows.UI.Notifications.NotificationMirroring value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotificationManager", "void ToastNotificationManager.ConfigureNotificationMirroring(NotificationMirroring value)");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.ToastNotificationManager.History.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.ToastNotifier CreateToastNotifier()
		{
			throw new global::System.NotImplementedException("The member ToastNotifier ToastNotificationManager.CreateToastNotifier() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Notifications.ToastNotifier CreateToastNotifier( string applicationId)
		{
			throw new global::System.NotImplementedException("The member ToastNotifier ToastNotificationManager.CreateToastNotifier(string applicationId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Data.Xml.Dom.XmlDocument GetTemplateContent( global::Windows.UI.Notifications.ToastTemplateType type)
		{
			throw new global::System.NotImplementedException("The member XmlDocument ToastNotificationManager.GetTemplateContent(ToastTemplateType type) is not implemented in Uno.");
		}
		#endif
	}
}
