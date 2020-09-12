#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.Telemetry
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlatformTelemetryRegistrationSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint UploadQuotaSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint PlatformTelemetryRegistrationSettings.UploadQuotaSize is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings", "uint PlatformTelemetryRegistrationSettings.UploadQuotaSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint StorageSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint PlatformTelemetryRegistrationSettings.StorageSize is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings", "uint PlatformTelemetryRegistrationSettings.StorageSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PlatformTelemetryRegistrationSettings() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings", "PlatformTelemetryRegistrationSettings.PlatformTelemetryRegistrationSettings()");
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings.PlatformTelemetryRegistrationSettings()
		// Forced skipping of method Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings.StorageSize.get
		// Forced skipping of method Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings.StorageSize.set
		// Forced skipping of method Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings.UploadQuotaSize.get
		// Forced skipping of method Windows.System.Diagnostics.Telemetry.PlatformTelemetryRegistrationSettings.UploadQuotaSize.set
	}
}
