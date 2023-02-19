#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NotificationBinding 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Template
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NotificationBinding.Template is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20NotificationBinding.Template");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.NotificationBinding", "string NotificationBinding.Template");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NotificationBinding.Language is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20NotificationBinding.Language");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.NotificationBinding", "string NotificationBinding.Language");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> Hints
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> NotificationBinding.Hints is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IDictionary%3Cstring%2C%20string%3E%20NotificationBinding.Hints");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.NotificationBinding.Template.get
		// Forced skipping of method Windows.UI.Notifications.NotificationBinding.Template.set
		// Forced skipping of method Windows.UI.Notifications.NotificationBinding.Language.get
		// Forced skipping of method Windows.UI.Notifications.NotificationBinding.Language.set
		// Forced skipping of method Windows.UI.Notifications.NotificationBinding.Hints.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Notifications.AdaptiveNotificationText> GetTextElements()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<AdaptiveNotificationText> NotificationBinding.GetTextElements() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CAdaptiveNotificationText%3E%20NotificationBinding.GetTextElements%28%29");
		}
		#endif
	}
}
