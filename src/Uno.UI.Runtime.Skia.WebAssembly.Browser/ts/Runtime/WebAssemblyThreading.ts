namespace Uno.UI.Runtime.Skia {
	export class WebAssemblyThreading {
		public static isThreadingEnabled() {
			return (<any>globalThis).crossOriginIsolated &&
				typeof SharedArrayBuffer !== undefined &&
				(<any>Module).PThread !== undefined;
		}

		public static getWindowObject() {
			return globalThis.window;
		}
	}
}
