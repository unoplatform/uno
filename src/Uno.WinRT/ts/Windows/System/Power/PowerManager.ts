declare class BatteryManager {
	charging: boolean;
	level: number;
	dischargingTime: number;

	addEventListener(
		type: "chargingchange" | "dischargingtimechange" | "levelchange",
		listener: (this: this, ev: Event) => any,
		useCapture?: boolean): void;

	removeEventListener(
		type: "chargingchange" | "dischargingtimechange" | "levelchange",
		listener: (this: this, ev: Event) => any,
		useCapture?: boolean): void;
}

interface Navigator {
	getBattery(): Promise<BatteryManager>;
}


namespace Windows.System.Power {

	export class PowerManager {

		private static battery: BatteryManager;
		private static dispatchChargingChanged: () => (void | Promise<void>);
		private static dispatchRemainingChargePercentChanged: () => (void | Promise<void>);
		private static dispatchRemainingDischargeTimeChanged: () => (void | Promise<void>);

		public static async initializeAsync(): Promise<string> {
			if (!PowerManager.battery) {
				PowerManager.battery = await navigator.getBattery();

				if ((<any>globalThis).Uno.UI.Runtime.Skia.WebAssemblyThreading.isThreadingEnabled()) {
					PowerManager.dispatchChargingChanged =
						(<any>globalThis).DotnetExports.Uno.Windows.System.Power.PowerManager.DispatchChargingChangedAsync;
					PowerManager.dispatchRemainingChargePercentChanged =
						(<any>globalThis).DotnetExports.Uno.Windows.System.Power.PowerManager.DispatchRemainingChargePercentChangedAsync;
					PowerManager.dispatchRemainingDischargeTimeChanged =
						(<any>globalThis).DotnetExports.Uno.Windows.System.Power.PowerManager.DispatchRemainingDischargeTimeChangedAsync;
				} else {
					PowerManager.dispatchChargingChanged = (<any>globalThis).DotnetExports.Uno.Windows.System.Power.PowerManager.DispatchChargingChanged;
					PowerManager.dispatchRemainingChargePercentChanged = (<any>globalThis).DotnetExports.Uno.Windows.System.Power.PowerManager.DispatchRemainingChargePercentChanged;
					PowerManager.dispatchRemainingDischargeTimeChanged = (<any>globalThis).DotnetExports.Uno.Windows.System.Power.PowerManager.DispatchRemainingDischargeTimeChanged;
				}
			}

			return null;
		}

		public static startChargingChange() {
			PowerManager.battery.addEventListener("chargingchange", PowerManager.onChargingChange);
		}

		public static endChargingChange() {
			PowerManager.battery.removeEventListener("chargingchange", PowerManager.onChargingChange);
		}

		public static startRemainingChargePercentChange() {
			PowerManager.battery.addEventListener("levelchange", PowerManager.onLevelChange);
		}

		public static endRemainingChargePercentChange() {
			PowerManager.battery.removeEventListener("levelchange", PowerManager.onLevelChange);
		}

		public static startRemainingDischargeTimeChange() {
			PowerManager.battery.addEventListener("dischargingtimechange", PowerManager.onDischargingTimeChange);
		}

		public static endRemainingDischargeTimeChange() {
			PowerManager.battery.removeEventListener("dischargingtimechange", PowerManager.onDischargingTimeChange);
		}

		public static getBatteryStatus(): string {
			if (PowerManager.battery) {
				return PowerManager.battery.charging ? "Charging" : "Discharging";
			}

			return "Idle";
		}

		public static getPowerSupplyStatus(): string {
			if (PowerManager.battery) {
				return PowerManager.battery.charging ? "Adequate" : "NotPresent";
			}

			return "NotPresent";
		}

		public static getRemainingChargePercent(): number {
			if (PowerManager.battery) {
				return PowerManager.battery.level;
			}

			return 1.0;
		}

		public static getRemainingDischargeTime(): number {
			if (PowerManager.battery) {
				const dischargingTime = PowerManager.battery.dischargingTime;
				if (Number.isFinite(dischargingTime)) {
					return dischargingTime;
				}
			}

			return -1;
		}

		private static onChargingChange() {
			PowerManager.dispatchChargingChanged();
		}

		private static onDischargingTimeChange() {
			PowerManager.dispatchRemainingDischargeTimeChanged();
		}

		private static onLevelChange() {
			PowerManager.dispatchRemainingChargePercentChanged();
		}
	}
}
