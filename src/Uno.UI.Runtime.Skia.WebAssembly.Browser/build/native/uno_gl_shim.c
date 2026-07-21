// GLCanvasElement resolves OpenGL entry points straight from emscripten's own C GL library
// (-lGL, shared with SkiaSharp) via uno_gl_resolve, instead of Uno's hand-written JS shim. This
// avoids the C->managed [UnmanagedCallersOnly] callback that traps under WASM full AOT (see
// unoplatform/kahua-private#520) and reuses emscripten's C-GL<->WebGL bridging (integer-id/handle
// tables, pointer/PBO marshaling, getter pointer-writes), which operates on the same shared
// context and window.GL.* tables the framework already uses.
//
// The one adaptation emscripten's GL does NOT do is promoting the unsized RGB/RGBA internal
// formats (excluded from WebGL2's color-renderable set) to their sized RGB8/RGBA8 forms, which the
// offscreen FBO color attachment relies on. glTexImage2D/glTexImage3D are therefore served from
// small wrappers that promote before forwarding to emscripten.
//
// Silk.NET still calli's into the resolved addresses (managed->native); the interpreter's invoke
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
