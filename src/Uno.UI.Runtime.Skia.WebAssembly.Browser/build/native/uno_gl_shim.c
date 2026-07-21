// gl functions with more than 8 arguments (glTexImage2D, glBlitFramebuffer, etc.) can't be
// dispatched to managed code on browser-wasm: [UnmanagedCallersOnly] callbacks cap at 8 args on
// the interpreter (dotnet/runtime#109338), and a C->managed callback resolved by EntryPoint name
// has no valid native-to-interp entry under full AOT (it traps with "null function or function
// signature mismatch" at the callback). See unoplatform/kahua-private#520.
//
// Instead, each shim forwards straight to the JS WebGL layer via EM_JS - no managed hop at all.
// Silk.NET still calli's into these C wrappers (managed->native), which the interpreter's invoke
// thunks handle in both interpreter and AOT builds. The framework hands Silk.NET the C wrapper's
// address via the uno_get_*_ptr getters (called through [DllImport("uno_gl_shim")]).
//
// The DllImport module name resolves because this file is linked via WasmShellNativeCompile
// -> NativeFileReference, and the wasm SDK registers every NativeFileReference's base name
// as a static PInvoke module (_WasmPInvokeModules in WasmApp.Common.targets) - the same
// mechanism that makes DllImport("unoicu") work against unoicu.a.
//
// The EM_JS bodies call the same JS entry points the rest of WasmGLFunctions uses
// (globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.*), so pointer arguments (heap offsets) are
// interpreted identically to the [JSImport] path used for the <=8-arg functions.

#include <emscripten.h>
#include <stdint.h>

// glTexImage2D: 9 args
EM_JS(void, uno_js_glTexImage2D, (int target, int level, int internalformat, int width, int height, int border, int format, int type, int pixels), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glTexImage2D(target, level, internalformat, width, height, border, format, type, pixels);
});
EMSCRIPTEN_KEEPALIVE
void uno_glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int pixels) {
	uno_js_glTexImage2D(target, level, internalformat, width, height, border, format, type, pixels);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexImage2D_ptr(void) { return (int)(intptr_t)&uno_glTexImage2D; }

// glTexSubImage2D: 9 args
EM_JS(void, uno_js_glTexSubImage2D, (int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int pixels), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glTexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels);
});
EMSCRIPTEN_KEEPALIVE
void uno_glTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int pixels) {
	uno_js_glTexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexSubImage2D_ptr(void) { return (int)(intptr_t)&uno_glTexSubImage2D; }

// glTexImage3D: 10 args
EM_JS(void, uno_js_glTexImage3D, (int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, int pixels), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glTexImage3D(target, level, internalformat, width, height, depth, border, format, type, pixels);
});
EMSCRIPTEN_KEEPALIVE
void uno_glTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, int pixels) {
	uno_js_glTexImage3D(target, level, internalformat, width, height, depth, border, format, type, pixels);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexImage3D_ptr(void) { return (int)(intptr_t)&uno_glTexImage3D; }

// glTexSubImage3D: 11 args
EM_JS(void, uno_js_glTexSubImage3D, (int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, int pixels), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glTexSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);
});
EMSCRIPTEN_KEEPALIVE
void uno_glTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, int pixels) {
	uno_js_glTexSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexSubImage3D_ptr(void) { return (int)(intptr_t)&uno_glTexSubImage3D; }

// glCopyTexSubImage3D: 9 args
EM_JS(void, uno_js_glCopyTexSubImage3D, (int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glCopyTexSubImage3D(target, level, xoffset, yoffset, zoffset, x, y, width, height);
});
EMSCRIPTEN_KEEPALIVE
void uno_glCopyTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) {
	uno_js_glCopyTexSubImage3D(target, level, xoffset, yoffset, zoffset, x, y, width, height);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCopyTexSubImage3D_ptr(void) { return (int)(intptr_t)&uno_glCopyTexSubImage3D; }

// glCompressedTexImage3D: 9 args
EM_JS(void, uno_js_glCompressedTexImage3D, (int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, int data), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glCompressedTexImage3D(target, level, internalformat, width, height, depth, border, imageSize, data);
});
EMSCRIPTEN_KEEPALIVE
void uno_glCompressedTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, int data) {
	uno_js_glCompressedTexImage3D(target, level, internalformat, width, height, depth, border, imageSize, data);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCompressedTexImage3D_ptr(void) { return (int)(intptr_t)&uno_glCompressedTexImage3D; }

// glCompressedTexSubImage2D: 9 args
EM_JS(void, uno_js_glCompressedTexSubImage2D, (int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, int data), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glCompressedTexSubImage2D(target, level, xoffset, yoffset, width, height, format, imageSize, data);
});
EMSCRIPTEN_KEEPALIVE
void uno_glCompressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, int data) {
	uno_js_glCompressedTexSubImage2D(target, level, xoffset, yoffset, width, height, format, imageSize, data);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCompressedTexSubImage2D_ptr(void) { return (int)(intptr_t)&uno_glCompressedTexSubImage2D; }

// glCompressedTexSubImage3D: 11 args
EM_JS(void, uno_js_glCompressedTexSubImage3D, (int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, int data), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glCompressedTexSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);
});
EMSCRIPTEN_KEEPALIVE
void uno_glCompressedTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, int data) {
	uno_js_glCompressedTexSubImage3D(target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCompressedTexSubImage3D_ptr(void) { return (int)(intptr_t)&uno_glCompressedTexSubImage3D; }

// glBlitFramebuffer: 10 args
EM_JS(void, uno_js_glBlitFramebuffer, (int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter), {
	globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.glBlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
});
EMSCRIPTEN_KEEPALIVE
void uno_glBlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter) {
	uno_js_glBlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glBlitFramebuffer_ptr(void) { return (int)(intptr_t)&uno_glBlitFramebuffer; }

// Dummy bodies for the SignaturePrimer [DllImport] declarations in
// WasmGLFunctions.LargeArityShims.cs. Those declarations prime the interpreter's managed->native
// trampoline cookies (including the 9/10/11-arg all-i32 signatures Silk.NET uses to calli the
// shim wrappers above); these C bodies exist solely so wasm-ld can resolve the symbols. They're
// never called at runtime - the managed callers sit behind a volatile-false guard.
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
