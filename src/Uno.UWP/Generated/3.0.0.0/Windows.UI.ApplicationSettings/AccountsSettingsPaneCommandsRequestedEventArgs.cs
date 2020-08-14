#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ApplicationSettings
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AccountsSettingsPaneCommandsRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string HeaderText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AccountsSettingsPaneCommandsRequestedEventArgs.HeaderText is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs", "string AccountsSettingsPaneCommandsRequestedEventArgs.HeaderText");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.UI.ApplicationSettings.SettingsCommand> Commands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<SettingsCommand> AccountsSettingsPaneCommandsRequestedEventArgs.Commands is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.UI.ApplicationSettings.CredentialCommand> CredentialCommands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<CredentialCommand> AccountsSettingsPaneCommandsRequestedEventArgs.CredentialCommands is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.UI.ApplicationSettings.WebAccountCommand> WebAccountCommands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<WebAccountCommand> AccountsSettingsPaneCommandsRequestedEventArgs.WebAccountCommands is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.UI.ApplicationSettings.WebAccountProviderCommand> WebAccountProviderCommands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<WebAccountProviderCommand> AccountsSettingsPaneCommandsRequestedEventArgs.WebAccountProviderCommands is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User AccountsSettingsPaneCommandsRequestedEventArgs.User is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs.WebAccountProviderCommands.get
		// Forced skipping of method Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs.WebAccountCommands.get
		// Forced skipping of method Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs.CredentialCommands.get
		// Forced skipping of method Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs.Commands.get
		// Forced skipping of method Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs.HeaderText.get
		// Forced skipping of method Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs.HeaderText.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ApplicationSettings.AccountsSettingsPaneEventDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member AccountsSettingsPaneEventDeferral AccountsSettingsPaneCommandsRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.ApplicationSettings.AccountsSettingsPaneCommandsRequestedEventArgs.User.get
	}
}
