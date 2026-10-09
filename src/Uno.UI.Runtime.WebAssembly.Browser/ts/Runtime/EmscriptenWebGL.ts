namespace Uno.UI.Runtime {
	// Shared helper for creating emscripten-managed WebGL contexts (window.GL) with a
	// WebGL 1.0 fallback. Used by both WebGlBrowserRenderer (the onscreen Skia render
	// surface) and WasmNativeOpenGLWrapper (GLCanvasElement's offscreen contexts).
	export class EmscriptenWebGL {
		public static assertGL(): any {
			const anyGL = (<any>window).GL;
			if (!anyGL || typeof anyGL.createContext !== "function") {
				throw "Emscripten GL module (window.GL) is unavailable";
			}
			return anyGL;
		}

		// attributeOverrides adjusts the defaults per call site (e.g. antialias for the
		// onscreen Skia surface, preserveDrawingBuffer for GLCanvasElement readbacks).
		public static createContext(canvas: HTMLCanvasElement, attributeOverrides: object = {}): number {
			const anyGL = EmscriptenWebGL.assertGL();
			const contextAttributes = {
				alpha: 1,
				depth: 1,
				stencil: 8,
				antialias: 0,
				premultipliedAlpha: 1,
				preserveDrawingBuffer: 0,
				preferLowPowerToHighPerformance: 0,
				failIfMajorPerformanceCaveat: 0,
				majorVersion: 2,
				minorVersion: 0,
				enableExtensionsByDefault: 1,
				explicitSwapControl: 0,
				renderViaOffscreenBackBuffer: 0,
				...attributeOverrides,
			};

			let ctx = anyGL.createContext(canvas, contextAttributes);
			if (!ctx && contextAttributes.majorVersion > 1) {
				console.warn("EmscriptenWebGL: falling back to WebGL 1.0");
				contextAttributes.majorVersion = 1;
				contextAttributes.minorVersion = 0;
				ctx = anyGL.createContext(canvas, contextAttributes);
			}

			const gl = ctx > 0 ? anyGL.getContext(ctx)?.GLctx : null;
			if (gl) {
				EmscriptenWebGL.fixStridedUploads(gl);
			}

			return ctx;
		}

		// With MAXIMUM_MEMORY above 2GB, emscripten avoids its garbage-free WebGL2 upload path (Firefox bug
		// 1838218) and slices the source as height * width rows, ignoring UNPACK_ROW_LENGTH and SKIP_*.
		// Skia uploads glyph atlas patches with a row length, so WebGL rejected them and glyphs went blank.
		// Re-slice such views to the strided size WebGL validates against. The 3D uploads aren't wrapped: Skia doesn't use them.
		public static fixStridedUploads(gl: WebGL2RenderingContext): void {
			const anyGl = <any>gl;
			if (anyGl.__unoStridedUploadsFixed || typeof WebGL2RenderingContext === "undefined" || !(gl instanceof WebGL2RenderingContext)) {
				return;
			}
			anyGl.__unoStridedUploadsFixed = true;

			const unpack = { alignment: 4, rowLength: 0, skipRows: 0, skipPixels: 0 };
			const pixelStorei = gl.pixelStorei;
			anyGl.pixelStorei = function (pname: number, param: number) {
				switch (pname) {
					case gl.UNPACK_ALIGNMENT: unpack.alignment = param; break;
					case gl.UNPACK_ROW_LENGTH: unpack.rowLength = param; break;
					case gl.UNPACK_SKIP_ROWS: unpack.skipRows = param; break;
					case gl.UNPACK_SKIP_PIXELS: unpack.skipPixels = param; break;
				}
				return pixelStorei.call(gl, pname, param);
			};

			const isStrided = () => unpack.rowLength !== 0 || unpack.skipRows !== 0 || unpack.skipPixels !== 0;

			const resize = (width: number, height: number, format: number, type: number, pixels: any): any => {
				if (!ArrayBuffer.isView(pixels)) {
					return pixels;
				}

				const bytesPerPixel = EmscriptenWebGL.bytesPerPixel(gl, format, type);
				if (!bytesPerPixel || width <= 0 || height <= 0) {
					return pixels;
				}

				const alignment = unpack.alignment;
				const rowBytes = Math.ceil(((unpack.rowLength || width) * bytesPerPixel) / alignment) * alignment;
				const required = (unpack.skipRows + height - 1) * rowBytes + (unpack.skipPixels + width) * bytesPerPixel;
				const view = <ArrayBufferView>pixels;
				if (view.byteLength >= required || view.byteOffset + required > view.buffer.byteLength) {
					return pixels;
				}

				// Rounding up to whole elements must not run past the end of the buffer.
				const elementSize = (<any>view).BYTES_PER_ELEMENT || 1;
				const length = Math.min(Math.ceil(required / elementSize), Math.floor((view.buffer.byteLength - view.byteOffset) / elementSize));
				return new (<any>view.constructor)(view.buffer, view.byteOffset, length);
			};

			const texImage2D = gl.texImage2D;
			anyGl.texImage2D = function () {
				// (target, level, internalformat, width, height, border, format, type, pixels)
				if (arguments.length !== 9 || !isStrided()) {
					return (<any>texImage2D).apply(gl, arguments);
				}
				const args = Array.prototype.slice.call(arguments);
				args[8] = resize(args[3], args[4], args[6], args[7], args[8]);
				return (<any>texImage2D).apply(gl, args);
			};

			const texSubImage2D = gl.texSubImage2D;
			anyGl.texSubImage2D = function () {
				// (target, level, xoffset, yoffset, width, height, format, type, pixels)
				if (arguments.length !== 9 || !isStrided()) {
					return (<any>texSubImage2D).apply(gl, arguments);
				}
				const args = Array.prototype.slice.call(arguments);
				args[8] = resize(args[4], args[5], args[6], args[7], args[8]);
				return (<any>texSubImage2D).apply(gl, args);
			};
		}

		private static bytesPerPixel(gl: WebGL2RenderingContext, format: number, type: number): number {
			switch (type) {
				case gl.UNSIGNED_SHORT_5_6_5:
				case gl.UNSIGNED_SHORT_4_4_4_4:
				case gl.UNSIGNED_SHORT_5_5_5_1:
					return 2;
				case gl.UNSIGNED_INT_2_10_10_10_REV:
				case gl.UNSIGNED_INT_10F_11F_11F_REV:
				case gl.UNSIGNED_INT_5_9_9_9_REV:
				case gl.UNSIGNED_INT_24_8:
					return 4;
			}

			let componentSize: number;
			switch (type) {
				case gl.UNSIGNED_BYTE:
				case gl.BYTE:
					componentSize = 1; break;
				case gl.UNSIGNED_SHORT:
				case gl.SHORT:
				case gl.HALF_FLOAT:
					componentSize = 2; break;
				case gl.UNSIGNED_INT:
				case gl.INT:
				case gl.FLOAT:
					componentSize = 4; break;
				default:
					return 0;
			}

			switch (format) {
				case gl.RED:
				case gl.RED_INTEGER:
				case gl.ALPHA:
				case gl.LUMINANCE:
				case gl.DEPTH_COMPONENT:
					return componentSize;
				case gl.RG:
				case gl.RG_INTEGER:
				case gl.LUMINANCE_ALPHA:
					return componentSize * 2;
				case gl.RGB:
				case gl.RGB_INTEGER:
					return componentSize * 3;
				case gl.RGBA:
				case gl.RGBA_INTEGER:
					return componentSize * 4;
				default:
					return 0;
			}
		}
	}
}
