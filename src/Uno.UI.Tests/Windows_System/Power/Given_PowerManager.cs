using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System.Power;

namespace Uno.UI.Tests.Windows_System.Power
{
	[TestClass]
	public class Given_PowerManager
	{
		[TestMethod]
		public void When_Mapping_Charging_Snapshot()
		{
			var snapshot = new PowerManager.PowerSourceSnapshot(
				IsPresent: true,
				IsExternalPower: true,
				IsCharging: true,
				CurrentCapacity: 42,
				MaxCapacity: 84);

			Assert.AreEqual(BatteryStatus.Charging, PowerManager.GetBatteryStatus(snapshot));
			Assert.AreEqual(PowerSupplyStatus.Adequate, PowerManager.GetPowerSupplyStatus(snapshot));
			Assert.AreEqual(50, PowerManager.GetRemainingChargePercent(snapshot));
		}

		[TestMethod]
		public void When_Mapping_Discharging_Snapshot()
		{
			var snapshot = new PowerManager.PowerSourceSnapshot(
				IsPresent: true,
				IsExternalPower: false,
				IsCharging: false,
				CurrentCapacity: 33,
				MaxCapacity: 100);

			Assert.AreEqual(BatteryStatus.Discharging, PowerManager.GetBatteryStatus(snapshot));
			Assert.AreEqual(PowerSupplyStatus.NotPresent, PowerManager.GetPowerSupplyStatus(snapshot));
			Assert.AreEqual(33, PowerManager.GetRemainingChargePercent(snapshot));
		}

		[TestMethod]
		public void When_Mapping_Absent_Battery_Snapshot()
		{
			var snapshot = new PowerManager.PowerSourceSnapshot(
				IsPresent: false,
				IsExternalPower: false,
				IsCharging: false,
				CurrentCapacity: 10,
				MaxCapacity: 20);

			Assert.AreEqual(BatteryStatus.NotPresent, PowerManager.GetBatteryStatus(snapshot));
			Assert.AreEqual(PowerSupplyStatus.NotPresent, PowerManager.GetPowerSupplyStatus(snapshot));
			Assert.AreEqual(0, PowerManager.GetRemainingChargePercent(snapshot));
		}
	}
}
