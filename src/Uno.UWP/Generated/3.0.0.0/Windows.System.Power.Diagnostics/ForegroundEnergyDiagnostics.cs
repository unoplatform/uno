#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Power.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ForegroundEnergyDiagnostics 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static double DeviceSpecificConversionFactor
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ForegroundEnergyDiagnostics.DeviceSpecificConversionFactor is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Power.Diagnostics.ForegroundEnergyDiagnostics.DeviceSpecificConversionFactor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static ulong ComputeTotalEnergyUsage()
		{
			throw new global::System.NotImplementedException("The member ulong ForegroundEnergyDiagnostics.ComputeTotalEnergyUsage() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ResetTotalEnergyUsage()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.Diagnostics.ForegroundEnergyDiagnostics", "void ForegroundEnergyDiagnostics.ResetTotalEnergyUsage()");
		}
		#endif
	}
}
