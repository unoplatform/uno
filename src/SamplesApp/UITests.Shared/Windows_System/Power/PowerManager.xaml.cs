using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UwpPowerManager = Windows.System.Power.PowerManager;

namespace UITests.Shared.Windows_System.Power
{
	[SampleControlInfo("Windows.System", "Power.PowerManager",
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

		private async void UwpPowerManager_PowerSupplyStatusChanged(object sender, object e)
		{
			await ExecuteOnUiThreadAsync(() =>
				PowerSupplyStatusOuptut.Text = UwpPowerManager.PowerSupplyStatus.ToString());
		}

		private async void UwpPowerManager_RemainingChargePercentChanged(object sender, object e)
		{
			await ExecuteOnUiThreadAsync(() =>
				RemainingChargePercentOutput.Text = UwpPowerManager.RemainingChargePercent.ToString());
		}

		private async void UwpPowerManager_EnergySaverStatusChanged(object sender, object e)
		{
			await ExecuteOnUiThreadAsync(() =>
				EnergySaverStatusOuptut.Text = UwpPowerManager.EnergySaverStatus.ToString());
		}

		private async void UwpPowerManager_BatteryStatusChanged(object sender, object e)
		{
			await ExecuteOnUiThreadAsync(() =>
				BatteryStatusOutput.Text = UwpPowerManager.BatteryStatus.ToString());
		}

		private async Task ExecuteOnUiThreadAsync(Action action) =>
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
	}
}
