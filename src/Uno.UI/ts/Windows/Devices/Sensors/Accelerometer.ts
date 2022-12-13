interface Window {
	DeviceMotionEvent():void;
}

namespace Windows.Devices.Sensors {

	export class Accelerometer {

		private static dispatchReading: (x:number, y:number, z:number) => number;

		public static initialize(): boolean {
			if (window.DeviceMotionEvent) {
				const exports = (<any>globalThis).DotnetExports?.Uno?.Windows?.Devices?.Sensors?.Accelerometer;

				if (exports !== undefined) {
					Accelerometer.dispatchReading = exports.DispatchReading;
				}
				else {
					this.dispatchReading = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Sensors.Accelerometer:DispatchReading");
				}

				return true;
			}
			return false;
		}

		public static startReading() {
			window.addEventListener("devicemotion", Accelerometer.readingChangedHandler);
		}

		public static stopReading() {
			window.removeEventListener("devicemotion", Accelerometer.readingChangedHandler);
		}

		private static readingChangedHandler(event: any) {
			Accelerometer.dispatchReading(
				event.accelerationIncludingGravity.x,
				event.accelerationIncludingGravity.y,
				event.accelerationIncludingGravity.z);
		}
	}
}
