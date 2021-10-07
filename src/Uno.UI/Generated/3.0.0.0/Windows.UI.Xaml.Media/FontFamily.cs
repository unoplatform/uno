#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class FontFamily 
	{
		// Skipping already declared property Source
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Media.FontFamily XamlAutoFontFamily
		{
			get
			{
				throw new global::System.NotImplementedException("The member FontFamily FontFamily.XamlAutoFontFamily is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Media.FontFamily.FontFamily(string)
		// Forced skipping of method Windows.UI.Xaml.Media.FontFamily.FontFamily(string)
		// Forced skipping of method Windows.UI.Xaml.Media.FontFamily.Source.get
		// Forced skipping of method Windows.UI.Xaml.Media.FontFamily.XamlAutoFontFamily.get
	}
}
