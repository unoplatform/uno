declare class Magnetometer {
	constructor(config: any);
	addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
}

namespace Windows.Devices.Sensors {

	export class MagnetometerSensor {

		private static dispatchReading: (magneticFieldX: number, magneticFieldY: number, magneticFieldZ: number) => number;		
		private static magnetometer: any;

		public static initialize(): boolean {
			try {
				console.log("typeof " + (typeof Magnetometer));
				if (typeof Magnetometer === "function") {
					this.dispatchReading = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Sensors.Magnetometer:DispatchReading");
					this.magnetometer = new Magnetometer({ referenceFrame: 'device' });
					return true;
				}
			} catch (error) {
				//sensor not available
				console.log('Magnetometer could not be initialized.');
			}
			return false;
		}

		public static startReading() {
			this.magnetometer.addEventLi1stener('reading', MagnetometerSensor.readingChangedHandler);
			this.magnetometer.start();
		}

		public static stopReading() {
			this.magnetometer.removeEventListener('reading', MagnetometerSensor.readingChangedHandler);
			this.magnetometer.stop();
		}

		private static readingChangedHandler(event: any) {
			MagnetometerSensor.dispatchReading(
				this.magnetometer.x,
				this.magnetometer.y,
				this.magnetometer.z);
		}
	}
}
