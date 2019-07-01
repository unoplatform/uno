interface Window {
	DeviceMotionEvent():void;
}

namespace Windows.Devices.Sensors {

	export class Accelerometer {

		private static dispatchReading: any;

		public static initialize(): boolean {
			if (window.DeviceMotionEvent) {
				this.dispatchReading = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Sensors.Accelerometer:DispatchReading");
				return true;
			}
			return false;
		}

		public static startReading() {
			window.addEventListener('devicemotion', this.readingChangedHandler);
		}

		public static stopReading() {
			window.removeEventListener('devicemotion', this.readingChangedHandler);
		}

		private static readingChangedHandler(event:any) {
			this.dispatchReading(
				event.accelerationIncludingGravity.x,
				event.accelerationIncludingGravity.y,
				event.accelerationIncludingGravity.z);
		}
	}
}
