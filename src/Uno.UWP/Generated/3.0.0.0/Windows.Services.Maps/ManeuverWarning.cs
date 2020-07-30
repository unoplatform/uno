#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ManeuverWarning 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.ManeuverWarningKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManeuverWarningKind ManeuverWarning.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.ManeuverWarningSeverity Severity
		{
			get
			{
				throw new global::System.NotImplementedException("The member ManeuverWarningSeverity ManeuverWarning.Severity is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.ManeuverWarning.Kind.get
		// Forced skipping of method Windows.Services.Maps.ManeuverWarning.Severity.get
	}
}
