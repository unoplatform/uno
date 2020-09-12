#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ApplicationProfile 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Phone.ApplicationModel.ApplicationProfileModes Modes
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationProfileModes ApplicationProfile.Modes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Phone.ApplicationModel.ApplicationProfile.Modes.get
	}
}
