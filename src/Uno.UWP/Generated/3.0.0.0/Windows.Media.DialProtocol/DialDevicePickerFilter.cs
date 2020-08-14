#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.DialProtocol
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DialDevicePickerFilter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> SupportedAppNames
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> DialDevicePickerFilter.SupportedAppNames is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.DialProtocol.DialDevicePickerFilter.SupportedAppNames.get
	}
}
