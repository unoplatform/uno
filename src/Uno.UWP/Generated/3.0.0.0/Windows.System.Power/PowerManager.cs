#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Power
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PowerManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.System.Power.BatteryStatus BatteryStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member BatteryStatus PowerManager.BatteryStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.System.Power.EnergySaverStatus EnergySaverStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member EnergySaverStatus PowerManager.EnergySaverStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.System.Power.PowerSupplyStatus PowerSupplyStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member PowerSupplyStatus PowerManager.PowerSupplyStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static int RemainingChargePercent
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PowerManager.RemainingChargePercent is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::System.TimeSpan RemainingDischargeTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan PowerManager.RemainingDischargeTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Power.PowerManager.EnergySaverStatus.get
		// Forced skipping of method Windows.System.Power.PowerManager.EnergySaverStatusChanged.add
		// Forced skipping of method Windows.System.Power.PowerManager.EnergySaverStatusChanged.remove
		// Forced skipping of method Windows.System.Power.PowerManager.BatteryStatus.get
		// Forced skipping of method Windows.System.Power.PowerManager.BatteryStatusChanged.add
		// Forced skipping of method Windows.System.Power.PowerManager.BatteryStatusChanged.remove
		// Forced skipping of method Windows.System.Power.PowerManager.PowerSupplyStatus.get
		// Forced skipping of method Windows.System.Power.PowerManager.PowerSupplyStatusChanged.add
		// Forced skipping of method Windows.System.Power.PowerManager.PowerSupplyStatusChanged.remove
		// Forced skipping of method Windows.System.Power.PowerManager.RemainingChargePercent.get
		// Forced skipping of method Windows.System.Power.PowerManager.RemainingChargePercentChanged.add
		// Forced skipping of method Windows.System.Power.PowerManager.RemainingChargePercentChanged.remove
		// Forced skipping of method Windows.System.Power.PowerManager.RemainingDischargeTime.get
		// Forced skipping of method Windows.System.Power.PowerManager.RemainingDischargeTimeChanged.add
		// Forced skipping of method Windows.System.Power.PowerManager.RemainingDischargeTimeChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> BatteryStatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.BatteryStatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.BatteryStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> EnergySaverStatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.EnergySaverStatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.EnergySaverStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> PowerSupplyStatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.PowerSupplyStatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.PowerSupplyStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> RemainingChargePercentChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.RemainingChargePercentChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.RemainingChargePercentChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::System.EventHandler<object> RemainingDischargeTimeChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.RemainingDischargeTimeChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", "event EventHandler<object> PowerManager.RemainingDischargeTimeChanged");
			}
		}
		#endif
	}
}
