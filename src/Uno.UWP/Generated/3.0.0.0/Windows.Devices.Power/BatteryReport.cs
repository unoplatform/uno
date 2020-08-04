#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Power
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BatteryReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int? ChargeRateInMilliwatts
		{
			get
			{
				throw new global::System.NotImplementedException("The member int? BatteryReport.ChargeRateInMilliwatts is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int? DesignCapacityInMilliwattHours
		{
			get
			{
				throw new global::System.NotImplementedException("The member int? BatteryReport.DesignCapacityInMilliwattHours is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int? FullChargeCapacityInMilliwattHours
		{
			get
			{
				throw new global::System.NotImplementedException("The member int? BatteryReport.FullChargeCapacityInMilliwattHours is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int? RemainingCapacityInMilliwattHours
		{
			get
			{
				throw new global::System.NotImplementedException("The member int? BatteryReport.RemainingCapacityInMilliwattHours is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Power.BatteryStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member BatteryStatus BatteryReport.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Power.BatteryReport.ChargeRateInMilliwatts.get
		// Forced skipping of method Windows.Devices.Power.BatteryReport.DesignCapacityInMilliwattHours.get
		// Forced skipping of method Windows.Devices.Power.BatteryReport.FullChargeCapacityInMilliwattHours.get
		// Forced skipping of method Windows.Devices.Power.BatteryReport.RemainingCapacityInMilliwattHours.get
		// Forced skipping of method Windows.Devices.Power.BatteryReport.Status.get
	}
}
