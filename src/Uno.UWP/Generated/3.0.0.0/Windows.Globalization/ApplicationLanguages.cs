#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization
{
	#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ApplicationLanguages 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<string> GetLanguagesForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<string> ApplicationLanguages.GetLanguagesForUser(User user) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride.get
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride.set
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.Languages.get
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.ManifestLanguages.get
	}
}
