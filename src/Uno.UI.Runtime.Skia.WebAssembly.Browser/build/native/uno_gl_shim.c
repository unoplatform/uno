// Workaround for dotnet/runtime#109338: [UnmanagedCallersOnly] on browser-wasm interpreter
// caps at 8 arguments. gl functions with more arguments (glTexImage2D, glBlitFramebuffer, etc.)
// are wrapped in native shims that pack the arguments into a struct and forward to a
// single-argument managed dispatcher.
//
// The framework calls the uno_get_*_ptr getters via [DllImport("uno_gl_shim")] so the
// function-pointer table in WasmGLFunctions can route Silk.NET's lookups at these native
// functions (which have stable all-i32 wasm signatures) rather than at >8-arg
// [UnmanagedCallersOnly] methods that the interpreter cannot build trampolines for.
//
// Each shim follows the same pattern:
//   1. struct of all args
//   2. EMSCRIPTEN_KEEPALIVE C wrapper with the full arg list
//   3. extern managed dispatcher with [UnmanagedCallersOnly(EntryPoint=...)] in C#
//      (see WasmGLFunctions.LargeArityShims.cs)
//   4. EMSCRIPTEN_KEEPALIVE getter exposing the function pointer
//
// The DllImport module name resolves because this file is linked via WasmShellNativeCompile
// -> NativeFileReference, and the wasm SDK registers every NativeFileReference's base name
// as a static PInvoke module (_WasmPInvokeModules in WasmApp.Common.targets) - the same
// mechanism that makes DllImport("unoicu") work against unoicu.a.

#include <emscripten.h>
#include <stdint.h>

// glTexImage2D: 9 args
typedef struct { int target, level, internalformat, width, height, border, format, type, pixels; } uno_gl_texImage2D_args;
extern void uno_glTexImage2D_managed(uno_gl_texImage2D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int pixels) {
	uno_gl_texImage2D_args args = { target, level, internalformat, width, height, border, format, type, pixels };
	uno_glTexImage2D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexImage2D_ptr(void) { return (int)(intptr_t)&uno_glTexImage2D; }

// glTexSubImage2D: 9 args
typedef struct { int target, level, xoffset, yoffset, width, height, format, type, pixels; } uno_gl_texSubImage2D_args;
extern void uno_glTexSubImage2D_managed(uno_gl_texSubImage2D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int pixels) {
	uno_gl_texSubImage2D_args args = { target, level, xoffset, yoffset, width, height, format, type, pixels };
	uno_glTexSubImage2D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexSubImage2D_ptr(void) { return (int)(intptr_t)&uno_glTexSubImage2D; }

// glTexImage3D: 10 args
typedef struct { int target, level, internalformat, width, height, depth, border, format, type, pixels; } uno_gl_texImage3D_args;
extern void uno_glTexImage3D_managed(uno_gl_texImage3D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, int pixels) {
	uno_gl_texImage3D_args args = { target, level, internalformat, width, height, depth, border, format, type, pixels };
	uno_glTexImage3D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexImage3D_ptr(void) { return (int)(intptr_t)&uno_glTexImage3D; }

// glTexSubImage3D: 11 args
typedef struct { int target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels; } uno_gl_texSubImage3D_args;
extern void uno_glTexSubImage3D_managed(uno_gl_texSubImage3D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, int pixels) {
	uno_gl_texSubImage3D_args args = { target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pixels };
	uno_glTexSubImage3D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glTexSubImage3D_ptr(void) { return (int)(intptr_t)&uno_glTexSubImage3D; }

// glCopyTexSubImage3D: 9 args
typedef struct { int target, level, xoffset, yoffset, zoffset, x, y, width, height; } uno_gl_copyTexSubImage3D_args;
extern void uno_glCopyTexSubImage3D_managed(uno_gl_copyTexSubImage3D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glCopyTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) {
	uno_gl_copyTexSubImage3D_args args = { target, level, xoffset, yoffset, zoffset, x, y, width, height };
	uno_glCopyTexSubImage3D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCopyTexSubImage3D_ptr(void) { return (int)(intptr_t)&uno_glCopyTexSubImage3D; }

// glCompressedTexImage3D: 9 args
typedef struct { int target, level, internalformat, width, height, depth, border, imageSize, data; } uno_gl_compressedTexImage3D_args;
extern void uno_glCompressedTexImage3D_managed(uno_gl_compressedTexImage3D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glCompressedTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, int data) {
	uno_gl_compressedTexImage3D_args args = { target, level, internalformat, width, height, depth, border, imageSize, data };
	uno_glCompressedTexImage3D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCompressedTexImage3D_ptr(void) { return (int)(intptr_t)&uno_glCompressedTexImage3D; }

// glCompressedTexSubImage2D: 9 args
typedef struct { int target, level, xoffset, yoffset, width, height, format, imageSize, data; } uno_gl_compressedTexSubImage2D_args;
extern void uno_glCompressedTexSubImage2D_managed(uno_gl_compressedTexSubImage2D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glCompressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, int data) {
	uno_gl_compressedTexSubImage2D_args args = { target, level, xoffset, yoffset, width, height, format, imageSize, data };
	uno_glCompressedTexSubImage2D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCompressedTexSubImage2D_ptr(void) { return (int)(intptr_t)&uno_glCompressedTexSubImage2D; }

// glCompressedTexSubImage3D: 11 args
typedef struct { int target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data; } uno_gl_compressedTexSubImage3D_args;
extern void uno_glCompressedTexSubImage3D_managed(uno_gl_compressedTexSubImage3D_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glCompressedTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, int data) {
	uno_gl_compressedTexSubImage3D_args args = { target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, data };
	uno_glCompressedTexSubImage3D_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glCompressedTexSubImage3D_ptr(void) { return (int)(intptr_t)&uno_glCompressedTexSubImage3D; }

// glBlitFramebuffer: 10 args
typedef struct { int srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter; } uno_gl_blitFramebuffer_args;
extern void uno_glBlitFramebuffer_managed(uno_gl_blitFramebuffer_args* args);
EMSCRIPTEN_KEEPALIVE
void uno_glBlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter) {
	uno_gl_blitFramebuffer_args args = { srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter };
	uno_glBlitFramebuffer_managed(&args);
}
EMSCRIPTEN_KEEPALIVE
int uno_get_glBlitFramebuffer_ptr(void) { return (int)(intptr_t)&uno_glBlitFramebuffer; }

// Dummy bodies for the SignaturePrimer [DllImport] declarations in
// WasmGLFunctions.LargeArityShims.cs. Those declarations are what the build-time
// ManagedToNativeGenerator scans to register the matching trampoline cookies; these C
// bodies exist solely so wasm-ld can resolve the symbols at link time. They're never
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
// All-int large-arity cookies for the >8-arg shim wrappers above: Silk.NET reaches them via
// calli with 9 (glTexImage2D & co), 10 (glTexImage3D, glBlitFramebuffer), and 11
// (glTexSubImage3D, glCompressedTexSubImage3D) i32 arguments.
void uno_dummy_V9I(int a, int b, int c, int d, int e, int f, int g, int h, int i) { (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h; (void)i; }
void uno_dummy_V10I(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j) { (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h; (void)i; (void)j; }
void uno_dummy_V11I(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k) { (void)a; (void)b; (void)c; (void)d; (void)e; (void)f; (void)g; (void)h; (void)i; (void)j; (void)k; }
