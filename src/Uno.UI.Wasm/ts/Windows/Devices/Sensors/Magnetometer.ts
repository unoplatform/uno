declare class Magnetometer {
	constructor(config: any);
	addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
}

interface Window {
	Magnetometer: Magnetometer;
}

namespace Windows.Devices.Sensors {

	export class Magnetometer {

		private static dispatchReading: (magneticFieldX: number, magneticFieldY: number, magneticFieldZ: number) => number;		
		private static magnetometer: any;

		public static initialize(): boolean {
			try {
				if (typeof window.Magnetometer === "function") {
					this.dispatchReading = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Sensors.Magnetometer:DispatchReading");
					let MagnetometerClass: any = window.Magnetometer;
					this.magnetometer = new MagnetometerClass({ referenceFrame: 'device' });
					return true;
				}
			} catch (error) {
				//sensor not available
				console.log("Magnetometer could not be initialized.");
			}
			return false;
		}

		public static startReading() {
			this.magnetometer.addEventListener("reading", Magnetometer.readingChangedHandler);
			this.magnetometer.start();
		}

		public static stopReading() {
			this.magnetometer.removeEventListener("reading", Magnetometer.readingChangedHandler);
			this.magnetometer.stop();
		}

		private static readingChangedHandler(event: any) {
			Magnetometer.dispatchReading(
				Magnetometer.magnetometer.x,
				Magnetometer.magnetometer.y,
				Magnetometer.magnetometer.z);
		}
	}
}
