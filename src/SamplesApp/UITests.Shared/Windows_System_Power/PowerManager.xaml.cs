using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using UwpPowerManager = Windows.System.Power.PowerManager;

namespace UITests.Shared.Windows_System_Power
{
	[SampleControlInfoAttribute("Windows.System.Power", "PowerManager",
		description: "Shows properties of Power manager and handles its events")]
	public sealed partial class PowerManager : UserControl
	{
		public PowerManager()
		{
			this.InitializeComponent();
			UwpPowerManager.BatteryStatusChanged += UwpPowerManager_BatteryStatusChanged;
			UwpPowerManager.EnergySaverStatusChanged += UwpPowerManager_EnergySaverStatusChanged;
			UwpPowerManager.RemainingChargePercentChanged += UwpPowerManager_RemainingChargePercentChanged;
			UwpPowerManager.PowerSupplyStatusChanged += UwpPowerManager_PowerSupplyStatusChanged;
			BatteryStatusOutput.Text = UwpPowerManager.BatteryStatus.ToString();
			EnergySaverStatusOuptut.Text = UwpPowerManager.EnergySaverStatus.ToString();
			RemainingChargePercentOutput.Text = UwpPowerManager.RemainingChargePercent.ToString();
			PowerSupplyStatusOuptut.Text = UwpPowerManager.PowerSupplyStatus.ToString();
		}

		private void UwpPowerManager_PowerSupplyStatusChanged(object sender, object e)
		{
			PowerSupplyStatusOuptut.Text = UwpPowerManager.PowerSupplyStatus.ToString();
		}

		private void UwpPowerManager_RemainingChargePercentChanged(object sender, object e)
		{
			RemainingChargePercentOutput.Text = UwpPowerManager.RemainingChargePercent.ToString();
		}

		private void UwpPowerManager_EnergySaverStatusChanged(object sender, object e)
		{
			EnergySaverStatusOuptut.Text = UwpPowerManager.EnergySaverStatus.ToString();
		}

		private void UwpPowerManager_BatteryStatusChanged(object sender, object e)
		{
			BatteryStatusOutput.Text = UwpPowerManager.BatteryStatus.ToString();
		}
	}
}
