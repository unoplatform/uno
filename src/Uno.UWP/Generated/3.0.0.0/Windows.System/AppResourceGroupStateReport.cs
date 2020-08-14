#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppResourceGroupStateReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.AppResourceGroupEnergyQuotaState EnergyQuotaState
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppResourceGroupEnergyQuotaState AppResourceGroupStateReport.EnergyQuotaState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.AppResourceGroupExecutionState ExecutionState
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppResourceGroupExecutionState AppResourceGroupStateReport.ExecutionState is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.AppResourceGroupStateReport.ExecutionState.get
		// Forced skipping of method Windows.System.AppResourceGroupStateReport.EnergyQuotaState.get
	}
}
