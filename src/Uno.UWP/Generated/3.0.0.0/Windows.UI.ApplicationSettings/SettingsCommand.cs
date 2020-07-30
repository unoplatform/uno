#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ApplicationSettings
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SettingsCommand : global::Windows.UI.Popups.IUICommand
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Label
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SettingsCommand.Label is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ApplicationSettings.SettingsCommand", "string SettingsCommand.Label");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Popups.UICommandInvokedHandler Invoked
		{
			get
			{
				throw new global::System.NotImplementedException("The member UICommandInvokedHandler SettingsCommand.Invoked is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ApplicationSettings.SettingsCommand", "UICommandInvokedHandler SettingsCommand.Invoked");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member object SettingsCommand.Id is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ApplicationSettings.SettingsCommand", "object SettingsCommand.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.ApplicationSettings.SettingsCommand AccountsCommand
		{
			get
			{
				throw new global::System.NotImplementedException("The member SettingsCommand SettingsCommand.AccountsCommand is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SettingsCommand( object settingsCommandId,  string label,  global::Windows.UI.Popups.UICommandInvokedHandler handler) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ApplicationSettings.SettingsCommand", "SettingsCommand.SettingsCommand(object settingsCommandId, string label, UICommandInvokedHandler handler)");
		}
		#endif
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.SettingsCommand(object, string, Windows.UI.Popups.UICommandInvokedHandler)
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.Label.get
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.Label.set
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.Invoked.get
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.Invoked.set
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.Id.get
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.Id.set
		// Forced skipping of method Windows.UI.ApplicationSettings.SettingsCommand.AccountsCommand.get
		// Processing: Windows.UI.Popups.IUICommand
	}
}
