#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreTextServicesConstants 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static char HiddenCharacter
		{
			get
			{
				throw new global::System.NotImplementedException("The member char CoreTextServicesConstants.HiddenCharacter is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Text.Core.CoreTextServicesConstants.HiddenCharacter.get
	}
}
