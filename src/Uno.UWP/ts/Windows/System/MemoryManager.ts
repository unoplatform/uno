namespace Windows.System {

	export class MemoryManager {

		static getAppMemoryUsage() {
			if (typeof Module === "object") {

				// Returns an approximate memory usage for the current wasm module.
				// Initial buffer size is determine by the initial wasm memory defined in
				// emscripten.
				return (<any>Module).HEAPU8.length;
			}
			return 0;
		}
	}
}
