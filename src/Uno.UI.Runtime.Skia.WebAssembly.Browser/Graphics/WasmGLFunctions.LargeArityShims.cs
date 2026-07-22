#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// The >8-arg gl functions are served from emscripten's native C GL by uno_gl_resolve (see
// WasmGLFunctions.GetProcAddress and build/native/uno_gl_shim.c). Silk.NET still uses calli to
// reach the resolved addresses; under the interpreter those managed->native calls need matching
// invoke trampolines, which the SignaturePrimer [DllImport]s below prime at build time
// (float-bearing and 9/10/11-arg all-i32 signatures). Tracked internally.
internal static unsafe partial class WasmGLFunctions
{
	// Signature primer: surfaces float-bearing calli signatures to the build-time
	// ManagedToNativeGenerator so it emits matching interp->native trampoline cookies.
	// Silk.NET's gl.ClearColor / gl.Uniform1f / gl.Uniform4f etc. are dispatched as
	// delegate*[unmanaged[Cdecl]] calli, and in interpreter mode those trap with
	// "CANNOT HANDLE INTERP ICALL SIG VFFFF" (dotnet/runtime#61156) unless a [DllImport]
	// with the same shape exists somewhere in the scanned assemblies. The native bodies are
	// no-op stubs in build/native/uno_gl_shim.c - the symbols must exist at link time
	// (wasm-ld is strict) but are never called at runtime.
	//
	// Note: ManagedToNativeGenerator caches its scan in obj/.../wasm/for-build/m2n_cache.txt;
	// if you add a new signature here and an incremental build doesn't pick it up, clean the
	// consuming app's obj folder.
	private static class SignaturePrimer
	{
		private const string ShimLibrary = "uno_gl_shim";

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VFFFF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVFFFF(float a, float b, float c, float d);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VIF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVIF(int a, float b);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VIFFFF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVIFFFF(int a, float b, float c, float d, float e);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVF(float a);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VFF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVFF(float a, float b);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VFI", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVFI(float a, int b);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VIFF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVIFF(int a, float b, float c);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VIFFF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVIFFF(int a, float b, float c, float d);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VIIF", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVIIF(int a, int b, float c);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VIIL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVIIL(int a, int b, long c);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_IIIL", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int DummyIIIL(int a, int b, long c);

		// All-int large-arity cookies for the C-shim wrappers themselves: Silk.NET invokes
		// uno_glTexImage2D & co via calli with 9/10/11 i32 args, and those interp->native
		// trampolines also only exist if a matching [DllImport] is discovered at build time.
		// (9 ints happened to be primed by other libraries' pinvokes; 10 and 11 were not,
		// aborting the runtime on the first glTexImage3D/glTexSubImage3D call.)
		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_V8I", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyV8I(int a, int b, int c, int d, int e, int f, int g, int h);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VIIFI", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVIIFI(int a, int b, float c, int d);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VD", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVD(double a);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_VDD", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyVDD(double a, double b);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_V9I", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyV9I(int a, int b, int c, int d, int e, int f, int g, int h, int i);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_V10I", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyV10I(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j);

		[DllImport(ShimLibrary, EntryPoint = "uno_dummy_V11I", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void DummyV11I(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k);
	}

	// Referenced from KeepSignaturePrimer behind an always-false volatile guard so the
	// IL trimmer keeps the SignaturePrimer DllImports without them ever being called. Never assigned
	// by design (always default false), hence the CS0649 suppression.
#pragma warning disable CS0649
	private static volatile bool _neverTrue;
#pragma warning restore CS0649

	private static void KeepSignaturePrimer()
	{
		if (_neverTrue)
		{
			SignaturePrimer.DummyVFFFF(0, 0, 0, 0);
			SignaturePrimer.DummyVIF(0, 0);
			SignaturePrimer.DummyVIFFFF(0, 0, 0, 0, 0);
			SignaturePrimer.DummyVF(0);
			SignaturePrimer.DummyVFF(0, 0);
			SignaturePrimer.DummyVFI(0, 0);
			SignaturePrimer.DummyVIFF(0, 0, 0);
			SignaturePrimer.DummyVIFFF(0, 0, 0, 0);
			SignaturePrimer.DummyVIIF(0, 0, 0);
			SignaturePrimer.DummyVIIL(0, 0, 0);
			_ = SignaturePrimer.DummyIIIL(0, 0, 0);
			SignaturePrimer.DummyV8I(0, 0, 0, 0, 0, 0, 0, 0);
			SignaturePrimer.DummyVIIFI(0, 0, 0, 0);
			SignaturePrimer.DummyVD(0);
			SignaturePrimer.DummyVDD(0, 0);
			SignaturePrimer.DummyV9I(0, 0, 0, 0, 0, 0, 0, 0, 0);
			SignaturePrimer.DummyV10I(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
			SignaturePrimer.DummyV11I(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		}
	}
}
