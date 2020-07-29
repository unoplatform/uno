#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.Telemetry
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlatformTelemetryClient 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationResult Register( string id)
		{
			throw new global::System.NotImplementedException("The member PlatformTelemetryRegistrationResult PlatformTelemetryClient.Register(string id) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationResult Register( string id,  global::Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings settings)
		{
			throw new global::System.NotImplementedException("The member PlatformTelemetryRegistrationResult PlatformTelemetryClient.Register(string id, PlatformTelemetryRegistrationSettings settings) is not implemented in Uno.");
		}
		#endif
	}
}
