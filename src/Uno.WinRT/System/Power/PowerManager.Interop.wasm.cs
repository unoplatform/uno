using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.__System.Power;

internal static partial class PowerManager
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Windows.System.Power.PowerManager.initializeAsync")]
		internal static partial Task InitializeAsync();

		[JSImport("globalThis.Windows.System.Power.PowerManager.startChargingChange")]
		internal static partial void StartChargingChange();

		[JSImport("globalThis.Windows.System.Power.PowerManager.endChargingChange")]
		internal static partial void EndChargingChange();

		[JSImport("globalThis.Windows.System.Power.PowerManager.startRemainingChargePercentChange")]
		internal static partial void StartRemainingChargePercent();

		[JSImport("globalThis.Windows.System.Power.PowerManager.endRemainingChargePercentChange")]
		internal static partial void EndRemainingChargePercent();

		[JSImport("globalThis.Windows.System.Power.PowerManager.startRemainingDischargeTimeChange")]
		internal static partial void StartRemainingDischargeTime();

		[JSImport("globalThis.Windows.System.Power.PowerManager.endRemainingDischargeTimeChange")]
		internal static partial void EndRemainingDischargedTime();

		[JSImport("globalThis.Windows.System.Power.PowerManager.getBatteryStatus")]
		internal static partial string GetBatteryStatus();

		[JSImport("globalThis.Windows.System.Power.PowerManager.getPowerSupplyStatus")]
		internal static partial string GetPowerSupplyStatus();

		[JSImport("globalThis.Windows.System.Power.PowerManager.getRemainingChargePercent")]
		internal static partial double GetRemainingChargePercent();

		[JSImport("globalThis.Windows.System.Power.PowerManager.getRemainingDischargeTime")]
		internal static partial double GetRemainingDischargeTime();
	}
}
