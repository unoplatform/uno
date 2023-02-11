#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Printers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IppTextWithLanguage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string IppTextWithLanguage.Language is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20IppTextWithLanguage.Language");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member string IppTextWithLanguage.Value is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20IppTextWithLanguage.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public IppTextWithLanguage( string language,  string text) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.IppTextWithLanguage", "IppTextWithLanguage.IppTextWithLanguage(string language, string text)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Printers.IppTextWithLanguage.IppTextWithLanguage(string, string)
		// Forced skipping of method Windows.Devices.Printers.IppTextWithLanguage.Language.get
		// Forced skipping of method Windows.Devices.Printers.IppTextWithLanguage.Value.get
	}
}
