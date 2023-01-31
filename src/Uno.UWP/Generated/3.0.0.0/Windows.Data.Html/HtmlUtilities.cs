#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Html
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class HtmlUtilities 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string ConvertToText( string html)
		{
			throw new global::System.NotImplementedException("The member string HtmlUtilities.ConvertToText(string html) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20HtmlUtilities.ConvertToText%28string%20html%29");
		}
		#endif
	}
}
