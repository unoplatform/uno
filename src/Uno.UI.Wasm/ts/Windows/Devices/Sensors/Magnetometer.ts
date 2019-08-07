declare class Magnetometer {
	constructor(config: any);
	addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
}

namespace Windows.Devices.Sensors {

	export class MagnetometerSensor {

		private static dispatchReading: (magneticFieldX: number, magneticFieldY: number, magneticFieldZ: number) => number;		
		private static magnetometer: any;

		public static initialize(): boolean {
			console.log("start initialize()");
			try {
				console.log("typeof " + (typeof Magnetometer));
				if (typeof Magnetometer === "function") {
					this.dispatchReading = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Sensors.Magnetometer:DispatchReading");
					this.magnetometer = new Magnetometer({ referenceFrame: 'device' });
					return true;
				}
			} catch (error) {
				//handles the case when sensor cannot be initialized
				console.log('Magnetometer could not be initialized.');
			}
			return false;
		}

		public static startReading() {
			console.log("start reading");
			this.magnetometer.addEventListener('reading', MagnetometerSensor.readingChangedHandler);
			this.magnetometer.start();
		}

		public static stopReading() {
			console.log("stop reading");
			this.magnetometer.removeEventListener('reading', MagnetometerSensor.readingChangedHandler);
			this.magnetometer.stop();
		}

		private static readingChangedHandler(event: any) {
			console.log("Reading gathered");
			MagnetometerSensor.dispatchReading(
				event.accelerationIncludingGravity.x,
				event.accelerationIncludingGravity.y,
				event.accelerationIncludingGravity.z);
		}
	}
}
