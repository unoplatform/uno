// Compile-injected into the consuming app's MAIN assembly by Uno.UI.Runtime.Skia.WebAssembly.Browser's
// build/targets when IsUnoHead == True. The [DllImport] declarations below surface a few
// float-bearing signatures to the wasm PInvokeTableGenerator so it emits matching interp->native
// trampoline cookies for them. Without this, Silk.NET's gl.ClearColor / gl.Uniform1f / gl.Uniform4f
// trap at runtime in interpreter mode with "CANNOT HANDLE INTERP ICALL SIG VFFFF" (dotnet/runtime#61156).
//
// Declaring them in the framework library didn't get them picked up; landing them in the head
// assembly does. The native bodies are no-op stubs in build/native/uno_gl_shim.c - the symbols
// must exist at link time (wasm-ld is strict) but are never called at runtime.
//
// Explicit Cdecl matches Silk.NET's delegate*[unmanaged[Cdecl]] cookie shape. Note also that
// ManagedToNativeGenerator caches its scan in obj/.../wasm/for-build/m2n_cache.txt; if you add
// a new signature here and an incremental build doesn't pick it up, clean the obj folder.

using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics.Generated;

internal static class UnoGLCanvasElementWasmSignaturePrimer
{
	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VFFFF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVFFFF(float a, float b, float c, float d);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VIF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVIF(int a, float b);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VIFFFF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVIFFFF(int a, float b, float c, float d, float e);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVF(float a);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VFF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVFF(float a, float b);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VFI", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVFI(float a, int b);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VIFF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVIFF(int a, float b, float c);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VIFFF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVIFFF(int a, float b, float c, float d);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VIIF", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVIIF(int a, int b, float c);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_VIIL", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DummyVIIL(int a, int b, long c);

	[DllImport("uno_gl_shim", EntryPoint = "uno_dummy_IIIL", CallingConvention = CallingConvention.Cdecl)]
	private static extern int DummyIIIL(int a, int b, long c);
}
