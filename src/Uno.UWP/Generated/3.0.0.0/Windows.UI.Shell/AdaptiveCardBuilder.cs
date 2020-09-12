#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Shell
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AdaptiveCardBuilder 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Shell.IAdaptiveCard CreateAdaptiveCardFromJson( string value)
		{
			throw new global::System.NotImplementedException("The member IAdaptiveCard AdaptiveCardBuilder.CreateAdaptiveCardFromJson(string value) is not implemented in Uno.");
		}
		#endif
	}
}
