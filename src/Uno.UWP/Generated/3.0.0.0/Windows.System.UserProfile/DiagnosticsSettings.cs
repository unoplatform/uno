#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.UserProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DiagnosticsSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanUseDiagnosticsToTailorExperiences
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DiagnosticsSettings.CanUseDiagnosticsToTailorExperiences is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20DiagnosticsSettings.CanUseDiagnosticsToTailorExperiences");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User DiagnosticsSettings.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20DiagnosticsSettings.User");
			}
		}
		#endif
		// Forced skipping of method Windows.System.UserProfile.DiagnosticsSettings.CanUseDiagnosticsToTailorExperiences.get
		// Forced skipping of method Windows.System.UserProfile.DiagnosticsSettings.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.UserProfile.DiagnosticsSettings GetDefault()
		{
			throw new global::System.NotImplementedException("The member DiagnosticsSettings DiagnosticsSettings.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DiagnosticsSettings%20DiagnosticsSettings.GetDefault%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.UserProfile.DiagnosticsSettings GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member DiagnosticsSettings DiagnosticsSettings.GetForUser(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DiagnosticsSettings%20DiagnosticsSettings.GetForUser%28User%20user%29");
		}
		#endif
	}
}
