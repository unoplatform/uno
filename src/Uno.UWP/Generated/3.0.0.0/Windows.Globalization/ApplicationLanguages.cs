#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public static partial class ApplicationLanguages 
	{
		// Skipping already declared property PrimaryLanguageOverride
		// Skipping already declared property Languages
		// Skipping already declared property ManifestLanguages
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<string> GetLanguagesForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<string> ApplicationLanguages.GetLanguagesForUser(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cstring%3E%20ApplicationLanguages.GetLanguagesForUser%28User%20user%29");
		}
		#endif
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride.get
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride.set
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.Languages.get
		// Forced skipping of method Windows.Globalization.ApplicationLanguages.ManifestLanguages.get
	}
}
