declare class BatteryManager {
	charging: boolean;
}

interface Navigator {
	getBattery(): Promise<BatteryManager>;
}


namespace Windows.System.Power {

	export class PowerManager {

		private static battery: BatteryManager;

		public static async initializeAsync(): Promise<string> {
			if (!PowerManager.battery) {
				PowerManager.battery = await navigator.getBattery();
			}

			return null;
		}

		public static getBatteryStatus(): string {
			if (PowerManager.battery) {
				return PowerManager.battery.charging ? "Charging" : "Discharging";
			}

			return "Idle";
		}
	}
}
