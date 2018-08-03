module Uno.UI.Interop {
	export interface IWebAssemblyModule {
		getValue(ptr: number, format: string): number;
		HEAPU8: Uint8Array;
		HEAP8: Int8Array;
		HEAP16: Int16Array;
		HEAPU16: Uint16Array;
		HEAP32: Int32Array;
		HEAPU32: Uint32Array;
		HEAPF32: Float32Array;
		HEAPF64: Float64Array;
	}
}
