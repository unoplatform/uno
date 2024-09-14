declare class AmbientLightSensor {
	constructor(config: any);
	addEventListener(type: "reading", listener: (this: this, ev: Event) => any): void;
	removeEventListener(type: "reading", listener: (this: this, ev: Event) => any): void;
	start(): void;
	stop(): void;
	illuminance: number;
}

interface Window {
	AmbientLightSensor: AmbientLightSensor;
}

namespace Windows.Devices.Sensors {

	export class LightSensor {

		private static dispatchReading: (lux: number) => number;
		private static ambientLightSensor: AmbientLightSensor;

		public static initialize(): boolean {
			try {
				if (typeof window.AmbientLightSensor === "function") {
					if ((<any>globalThis).DotnetExports !== undefined) {
						LightSensor.dispatchReading = (<any>globalThis).DotnetExports.Uno.Windows.Devices.Sensors.LightSensor.DispatchReading;
					} else {
						throw `Unable to find dotnet exports`;
					}
					const AmbientLightSensorClass: any = window.AmbientLightSensor;
					LightSensor.ambientLightSensor = new AmbientLightSensorClass();
					return true;
				}
			} catch (error) {
				// Sensor not available
				console.error("AmbientLightSensor could not be initialized.");
			}
			return false;
		}

		public static startReading() {
			LightSensor.ambientLightSensor.addEventListener("reading", LightSensor.readingChangedHandler);
			LightSensor.ambientLightSensor.start();
		}

		public static stopReading() {
			LightSensor.ambientLightSensor.removeEventListener("reading", LightSensor.readingChangedHandler);
			LightSensor.ambientLightSensor.stop();
		}

		private static readingChangedHandler(event: any) {
			LightSensor.dispatchReading(LightSensor.ambientLightSensor.illuminance);
		}
	}
}
