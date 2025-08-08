interface Window {
	DeviceMotionEvent():void;
}

namespace Windows.Devices.Sensors {

	export class Accelerometer {

		private static dispatchReading: (x:number, y:number, z:number) => number;

		public static initialize(): boolean {
			if (window.DeviceMotionEvent) {
				const exports = (<any>globalThis).DotnetExports?.Uno?.Uno?.Devices?.Sensors?.Accelerometer;

				if (exports !== undefined) {
					Accelerometer.dispatchReading = exports.DispatchReading;
				}
				else {
					throw `Accelerometer: Unable to find dotnet exports`;
				}

				// Check if accelerometer sensor is actually available
				return Accelerometer.testSensorAvailability();
			}
			return false;
		}

		private static testSensorAvailability(): boolean {
			try {
				// Primary approach: Use device/platform detection heuristics first
				// Check if we're on a device that typically has accelerometers
				const userAgent = navigator.userAgent.toLowerCase();
				const isMobile = /android|iphone|ipad|ipod|blackberry|iemobile|opera mini|mobile/i.test(userAgent);
				const hasTouch = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
				
				// Desktop browsers typically don't have accelerometers
				// Even if the Generic Sensor API is available, hardware likely isn't present
				if (!isMobile && !hasTouch) {
					return false;
				}

				// For mobile/tablet devices, verify that sensor APIs are available
				// Try Generic Sensor API first (modern approach)
				if ('Accelerometer' in window) {
					try {
						// Test if we can create an Accelerometer instance
						const testSensor = new (window as any).Accelerometer({ frequency: 1 });
						testSensor.start();
						testSensor.stop();
						return true;
					} catch (error) {
						// Sensor not available or permission denied, fall through to DeviceMotionEvent check
					}
				}

				// Fallback to DeviceMotionEvent for mobile devices
				// If we reach here, we're on a mobile/touch device but Generic Sensor API failed
				// Mobile devices virtually always have accelerometers, so assume it's available
				return true;
				
			} catch (error) {
				console.log("Accelerometer sensor availability test failed:", error);
				return false;
			}
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
