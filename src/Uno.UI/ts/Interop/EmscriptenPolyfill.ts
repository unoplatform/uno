namespace Uno.UI.Interop {
	export class Emscripten {

		static assert(x: any, message: any) {
			if (!x) throw new Error(message);
		}

		static warnOnce(a: any, msg: any = null) {
			if (!msg) {
				msg = a;
				a = false;
			}
			if (!a) {
				(<any>Emscripten).msgs ||= {};
				if (msg in (<any>Emscripten).msgs) return;
				(<any>Emscripten).msgs[msg] = true;
				console.warn(msg);
			}
		}

		// Copy of the stringToUTF8 function from the emscripten library
		static stringToUTF8Array(str: any, heap: any, outIdx: any, maxBytesToWrite: any): number {
			if (!(maxBytesToWrite > 0))
				return 0;
			var startIdx = outIdx;
			var endIdx = outIdx + maxBytesToWrite - 1;
			for (var i = 0; i < str.length; ++i) {
				var u = str.charCodeAt(i);
				if (u >= 55296 && u <= 57343) {
					var u1 = str.charCodeAt(++i);
					u = 65536 + ((u & 1023) << 10) | u1 & 1023
				}
				if (u <= 127) {
					if (outIdx >= endIdx)
						break;
					heap[outIdx++] = u
				} else if (u <= 2047) {
					if (outIdx + 1 >= endIdx)
						break;
					heap[outIdx++] = 192 | u >> 6;
					heap[outIdx++] = 128 | u & 63
				} else if (u <= 65535) {
					if (outIdx + 2 >= endIdx)
						break;
					heap[outIdx++] = 224 | u >> 12;
					heap[outIdx++] = 128 | u >> 6 & 63;
					heap[outIdx++] = 128 | u & 63
				} else {
					if (outIdx + 3 >= endIdx)
						break;
					if (u > 1114111)
						Emscripten.warnOnce("Invalid Unicode code point " + (<any>globalThis).Module.ptrToString(u) + " encountered when serializing a JS string to a UTF-8 string in wasm memory! (Valid unicode code points should be in range 0-0x10FFFF).");
					heap[outIdx++] = 240 | u >> 18;
					heap[outIdx++] = 128 | u >> 12 & 63;
					heap[outIdx++] = 128 | u >> 6 & 63;
					heap[outIdx++] = 128 | u & 63
				}
			}
			heap[outIdx] = 0;
			return outIdx - startIdx
		}

		public static stringToUTF8(str: any, outPtr: any, maxBytesToWrite: any) {
			Emscripten.assert(typeof maxBytesToWrite == "number", "stringToUTF8(str, outPtr, maxBytesToWrite) is missing the third parameter that specifies the length of the output buffer!");
			return Emscripten.stringToUTF8Array(str, Module.HEAPU8, outPtr, maxBytesToWrite)
		}
	}
}

if ((<any>globalThis).stringToUTF8 == undefined) {
	(<any>globalThis).stringToUTF8 = Uno.UI.Interop.Emscripten.stringToUTF8;
}
