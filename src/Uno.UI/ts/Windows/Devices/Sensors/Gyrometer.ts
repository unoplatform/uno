declare class Gyroscope {
	constructor(config: any);
	addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
}

interface Window {
	Gyroscope: Gyroscope;
}

namespace Windows.Devices.Sensors {

	export class Gyrometer {

		private static dispatchReading: (x: number, y: number, z: number) => number;		
		private static gyroscope: any;

		public static initialize(): boolean {
			try {
				if (typeof window.Gyroscope === "function") {
					if ((<any>globalThis).DotnetExports !== undefined) {
						this.dispatchReading = (<any>globalThis).DotnetExports.Uno.Windows.Devices.Sensors.Gyrometer.DispatchReading;
					} else {
						throw `Unable to find dotnet exports`;
					}
					let GyroscopeClass: any = window.Gyroscope;
					this.gyroscope = new GyroscopeClass({ referenceFrame: "device" });
					return true;
				}
			} catch (error) {
				//sensor not available
				console.log("Gyroscope could not be initialized.");
			}
			return false;
		}

		public static startReading() {
			this.gyroscope.addEventListener("reading", Gyrometer.readingChangedHandler);
			this.gyroscope.start();
		}

		public static stopReading() {
			this.gyroscope.removeEventListener("reading", Gyrometer.readingChangedHandler);
			this.gyroscope.stop();
		}

		private static readingChangedHandler(event: any) {
			Gyrometer.dispatchReading(
				Gyrometer.gyroscope.x,
				Gyrometer.gyroscope.y,
				Gyrometer.gyroscope.z);
		}
	}
}
