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
					this.dispatchReading = (<any>Module).mono_bind_static_method("[Uno] Windows.Devices.Sensors.LightSensor:DispatchReading");
					let AmbientLightSensorClass: any = window.AmbientLightSensor;
					LightSensor.ambientLightSensor = new AmbientLightSensorClass();
					return true;
				}
			} catch (error) {
				//sensor not available
				console.log("AmbientLightSensor could not be initialized.");
			}
			return false;
		}

		public static startReading() {
			this.ambientLightSensor.addEventListener("reading", LightSensor.readingChangedHandler);
			this.ambientLightSensor.start();
		}

		public static stopReading() {
			this.ambientLightSensor.removeEventListener("reading", LightSensor.readingChangedHandler);
			this.ambientLightSensor.stop();
		}

		private static readingChangedHandler(event: any) {
			LightSensor.dispatchReading(LightSensor.ambientLightSensor.illuminance);
		}
	}
}
