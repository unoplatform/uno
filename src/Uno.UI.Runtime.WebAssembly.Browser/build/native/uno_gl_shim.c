// GLCanvasElement resolves OpenGL entry points straight from emscripten's own C GL library
// (-lGL, shared with SkiaSharp) via uno_gl_resolve, instead of Uno's hand-written JS shim. This
// avoids the C->managed [UnmanagedCallersOnly] callback that traps under WASM full AOT (tracked
// internally) and reuses emscripten's C-GL<->WebGL bridging (integer-id/handle
// tables, pointer/PBO marshaling, getter pointer-writes), which operates on the same shared
// context and window.GL.* tables the framework already uses.
//
// The one adaptation emscripten's GL does NOT do is promoting the unsized RGB/RGBA internal
// formats (excluded from WebGL2's color-renderable set) to their sized RGB8/RGBA8 forms, which the
// offscreen FBO color attachment relies on. glTexImage2D/glTexImage3D are therefore served from
// small wrappers that promote before forwarding to emscripten.
//
// Silk.NET still uses calli to reach the resolved addresses (managed->native); the interpreter's invoke
// thunks handle that in both interpreter and AOT builds. The float/large-arity signatures are
// primed by the SignaturePrimer [DllImport]s (WasmGLFunctions.LargeArityShims.cs), whose native
// bodies are the uno_dummy_* stubs below.

#include <GLES3/gl3.h>
#include <emscripten.h>
#include <emscripten/html5_webgl.h>
#include <string.h>
#include <stdint.h>

#define UNO_PTR(x) ((const void*)(intptr_t)(x))

// WebGL2 color-renderability: unsized RGB/RGBA aren't renderable; promote to the sized forms when
// the pixel type is UNSIGNED_BYTE (the only case the framework and samples rely on).
static int uno_promote_internalformat(int internalformat, int type)
{
	if (type == GL_UNSIGNED_BYTE)
	{
		if (internalformat == GL_RGB) return GL_RGB8;
		if (internalformat == GL_RGBA) return GL_RGBA8;
	}
	return internalformat;
}

EMSCRIPTEN_KEEPALIVE
void uno_glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int pixels) {
	glTexImage2D(target, level, uno_promote_internalformat(internalformat, type), width, height, border, format, type, UNO_PTR(pixels));
}

EMSCRIPTEN_KEEPALIVE
void uno_glTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, int pixels) {
	glTexImage3D(target, level, uno_promote_internalformat(internalformat, type), width, height, depth, border, format, type, UNO_PTR(pixels));
}

// GLCanvasElement's offscreen context. Created, bound and destroyed from JS that runs in the calling
// thread's own realm, so under threading the handle lives in the UI thread's GL table, where the
// native GL calls look it up. A JSImport would run on the browser main thread instead.
EM_JS(int, uno_gl_js_create_context, (), {
	var canvas = typeof document !== "undefined" ? document.createElement("canvas") : new OffscreenCanvas(1, 1);
	canvas.width = 1;
	canvas.height = 1;

	// GLCanvasElement reads pixels back from an FBO, so the drawing buffer is never shown.
	var attrs = {
		alpha: 1, depth: 1, stencil: 8, antialias: 0,
		premultipliedAlpha: 1, preserveDrawingBuffer: 1,
		preferLowPowerToHighPerformance: 0, failIfMajorPerformanceCaveat: 0,
		majorVersion: 2, minorVersion: 0,
		enableExtensionsByDefault: 1, explicitSwapControl: 0,
		renderViaOffscreenBackBuffer: 0,
	};

	var ctx = GL.createContext(canvas, attrs);
	if (!ctx) {
		attrs.majorVersion = 1;
		ctx = GL.createContext(canvas, attrs);
	}
	return ctx > 0 ? ctx : 0;
});

// Returns the previous handle, or -1 when the handle is unknown to this thread.
EM_JS(int, uno_gl_js_make_current, (int handle), {
	var previous = GL.currentContext ? GL.currentContext.handle : 0;
	return GL.makeContextCurrent(handle) ? previous : -1;
});

EM_JS(void, uno_gl_js_destroy_context, (int handle), {
	if (GL.currentContext && GL.currentContext.handle === handle) {
		GL.makeContextCurrent(0);
	}
	GL.deleteContext(handle);
});

// With MAXIMUM_MEMORY above 2GB, emscripten avoids its garbage-free WebGL2 upload path (Firefox bug
// 1838218) and slices the source as height * width rows, ignoring UNPACK_ROW_LENGTH and SKIP_*.
// Skia uploads glyph atlas patches with a row length, so WebGL rejected them and glyphs went blank.
// Re-slice such views to the strided size WebGL validates against. The 3D uploads aren't wrapped:
// Skia doesn't use them. Lives here because every thread loads dotnet.native.js, so the main surface,
// the render worker and GLCanvasElement share this one copy.
// Installed on the realm's global, where the runtime tests reach it with a context of their own.
EM_JS(void, uno_gl_js_install_strided_fix, (), {
	if (globalThis.unoGlFixStridedUploads) {
		return;
	}

	globalThis.unoGlFixStridedUploads = function (gl) {
		if (!gl || gl.__unoStridedUploadsFixed || typeof gl.UNPACK_ROW_LENGTH !== "number") {
			return;
		}
		gl.__unoStridedUploadsFixed = true;

		var bytesPerPixel = function (format, type) {
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

			var componentSize;
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
		};

		var unpack = { alignment: 4, rowLength: 0, skipRows: 0, skipPixels: 0 };
		var pixelStorei = gl.pixelStorei;
		gl.pixelStorei = function (pname, param) {
			switch (pname) {
				case gl.UNPACK_ALIGNMENT: unpack.alignment = param; break;
				case gl.UNPACK_ROW_LENGTH: unpack.rowLength = param; break;
				case gl.UNPACK_SKIP_ROWS: unpack.skipRows = param; break;
				case gl.UNPACK_SKIP_PIXELS: unpack.skipPixels = param; break;
			}
			return pixelStorei.call(gl, pname, param);
		};

		var isStrided = function () {
			return unpack.rowLength !== 0 || unpack.skipRows !== 0 || unpack.skipPixels !== 0;
		};

		var resize = function (width, height, format, type, pixels) {
			if (!ArrayBuffer.isView(pixels)) {
				return pixels;
			}

			var bpp = bytesPerPixel(format, type);
			if (!bpp || width <= 0 || height <= 0) {
				return pixels;
			}

			var alignment = unpack.alignment;
			var rowBytes = Math.ceil(((unpack.rowLength || width) * bpp) / alignment) * alignment;
			var required = (unpack.skipRows + height - 1) * rowBytes + (unpack.skipPixels + width) * bpp;
			if (pixels.byteLength >= required || pixels.byteOffset + required > pixels.buffer.byteLength) {
				return pixels;
			}

			// Rounding up to whole elements must not run past the end of the buffer.
			var elementSize = pixels.BYTES_PER_ELEMENT || 1;
			var length = Math.min(Math.ceil(required / elementSize), Math.floor((pixels.buffer.byteLength - pixels.byteOffset) / elementSize));
			return new pixels.constructor(pixels.buffer, pixels.byteOffset, length);
		};

		var texImage2D = gl.texImage2D;
		gl.texImage2D = function () {
			// (target, level, internalformat, width, height, border, format, type, pixels)
			if (arguments.length !== 9 || !isStrided()) {
				return texImage2D.apply(gl, arguments);
			}
			var args = Array.prototype.slice.call(arguments);
			args[8] = resize(args[3], args[4], args[6], args[7], args[8]);
			return texImage2D.apply(gl, args);
		};

		var texSubImage2D = gl.texSubImage2D;
		gl.texSubImage2D = function () {
			// (target, level, xoffset, yoffset, width, height, format, type, pixels)
			if (arguments.length !== 9 || !isStrided()) {
				return texSubImage2D.apply(gl, arguments);
			}
			var args = Array.prototype.slice.call(arguments);
			args[8] = resize(args[4], args[5], args[6], args[7], args[8]);
			return texSubImage2D.apply(gl, args);
		};
	};
});

EM_JS(void, uno_gl_js_fix_strided_uploads, (int handle), {
	var context = GL.getContext(handle);
	if (context) {
		globalThis.unoGlFixStridedUploads(context.GLctx);
	}
});

// The browser main thread has no GL context of its own under threading, so it gets the global at startup.
__attribute__((constructor))
static void uno_gl_init(void) { uno_gl_js_install_strided_fix(); }

EMSCRIPTEN_KEEPALIVE
void uno_gl_fix_strided_uploads(int handle)
{
	uno_gl_js_install_strided_fix();
	uno_gl_js_fix_strided_uploads(handle);
}

EMSCRIPTEN_KEEPALIVE
int uno_gl_create_context(void)
{
	int handle = uno_gl_js_create_context();
	if (handle != 0)
	{
		uno_gl_fix_strided_uploads(handle);
	}
	return handle;
}

EMSCRIPTEN_KEEPALIVE
int uno_gl_make_current(int handle) { return uno_gl_js_make_current(handle); }

EMSCRIPTEN_KEEPALIVE
void uno_gl_destroy_context(int handle) { uno_gl_js_destroy_context(handle); }

// Central resolver: emscripten's native GL for every entry point, with the two promotion wrappers
// substituted for the calls that need WebGL2 unsized->sized internal-format promotion. Returns a
// wasm function-table index (0 = not available, the framework then falls back to its JS shim).
EMSCRIPTEN_KEEPALIVE
int uno_gl_resolve(const char* name) {
	if (strcmp(name, "glTexImage2D") == 0) return (int)(intptr_t)&uno_glTexImage2D;
	if (strcmp(name, "glTexImage3D") == 0) return (int)(intptr_t)&uno_glTexImage3D;
	return (int)(intptr_t)emscripten_webgl_get_proc_address(name);
}

// Dummy bodies for the SignaturePrimer [DllImport] declarations in
// WasmGLFunctions.LargeArityShims.cs. They prime the interpreter's managed->native trampoline
// cookies (float-bearing and 9/10/11-arg all-i32 signatures Silk.NET uses to calli the resolved
// GL entry points); these C bodies exist solely so wasm-ld can resolve the symbols. They're never
// called at runtime - the managed callers sit behind a volatile-false guard.
void uno_dummy_VFFFF(float a, float b, float c, float d) { (void)a; (void)b; (void)c; (void)d; }
void uno_dummy_VIF(int a, float b) { (void)a; (void)b; }
void uno_dummy_VIFFFF(int a, float b, float c, float d, float e) { (void)a; (void)b; (void)c; (void)d; (void)e; }
void uno_dummy_VF(float a) { (void)a; }
void uno_dummy_VFF(float a, float b) { (void)a; (void)b; }
void uno_dummy_VFI(float a, int b) { (void)a; (void)b; }
void uno_dummy_VIFF(int a, float b, float c) { (void)a; (void)b; (void)c; }
void uno_dummy_VIFFF(int a, float b, float c, float d) { (void)a; (void)b; (void)c; (void)d; }
void uno_dummy_VIIF(int a, int b, float c) { (void)a; (void)b; (void)c; }
void uno_dummy_VIIL(int a, int b, long long c) { (void)a; (void)b; (void)c; }
int uno_dummy_IIIL(int a, int b, long long c) { (void)a; (void)b; (void)c; return 0; }
void uno_dummy_VIIFI(int a, int b, float c, int d) { (void)a; (void)b; (void)c; (void)d; }
void uno_dummy_VD(double a) { (void)a; }
void uno_dummy_VDD(double a, double b) { (void)a; (void)b; }
void uno_dummy_V8I(int a, int b, int c, int d, int e, int f, int g, int h) { (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h; }
void uno_dummy_V9I(int a, int b, int c, int d, int e, int f, int g, int h, int i) { (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h; (void)i; }
void uno_dummy_V10I(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j) { (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h; (void)i; (void)j; }
void uno_dummy_V11I(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k) { (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h; (void)i; (void)j; (void)k; }
