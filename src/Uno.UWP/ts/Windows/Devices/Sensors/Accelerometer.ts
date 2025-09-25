declare class Accelerometer {
	constructor(config: any);
	addEventListener(type: "reading" | "activate", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
	removeEventListener(type: "reading", listener: (this: this, ev: Event) => any, useCapture?: boolean): void;
	start(): void;
	stop(): void;
	x: number;
	y: number;
	z: number;
}

interface Window {
	Accelerometer: typeof Accelerometer;
}

namespace Windows.Devices.Sensors {

	export class Accelerometer {

		private static dispatchReading: (x: number, y: number, z: number) => number;
		private static accelerometer: any;

		public static initialize(): boolean {
			try {
				if (typeof window.Accelerometer === "function") {
					if ((<any>globalThis).DotnetExports !== undefined) {
						this.dispatchReading = (<any>globalThis).DotnetExports.Uno.Windows.Devices.Sensors.Accelerometer.DispatchReading;
					} else {
						throw `Accelerometer: Unable to find dotnet exports`;
					}
					let AccelerometerClass: any = window.Accelerometer;
					this.accelerometer = new AccelerometerClass({ frequency: 60 });
					return true;
				}
			} catch (error) {
				//sensor not available
				console.log("Accelerometer could not be initialized:", error);
			}
			return false;
		}

		public static startReading() {
			this.accelerometer.addEventListener("reading", Accelerometer.readingChangedHandler);
			this.accelerometer.start();
		}

		public static stopReading() {
			this.accelerometer.removeEventListener("reading", Accelerometer.readingChangedHandler);
			this.accelerometer.stop();
		}

		private static readingChangedHandler(event: any) {
			Accelerometer.dispatchReading(
				Accelerometer.accelerometer.x,
				Accelerometer.accelerometer.y,
				Accelerometer.accelerometer.z);
		}
	}
}
