#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Gl functions with more than 8 arguments. The mono-wasm interpreter caps
// [UnmanagedCallersOnly] trampolines at 8 args (dotnet/runtime#109338), so these can't be
// dispatched directly from managed code. Each is wrapped by a C shim (build/native/uno_gl_shim.c)
// that packs the args into a struct and calls a 1-arg managed dispatcher.
//
// The dispatch table entry points to the C wrapper's function-table index, retrieved at static
// init time via the [DllImport("uno_gl_shim")] getters in NativeShimGetters below.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterLargeArityShimEntries()
	{
		KeepSignaturePrimer();

		_addresses["glTexSubImage2D"] = (IntPtr)NativeShimGetters.uno_get_glTexSubImage2D_ptr();
		_addresses["glTexImage3D"] = (IntPtr)NativeShimGetters.uno_get_glTexImage3D_ptr();
		_addresses["glTexSubImage3D"] = (IntPtr)NativeShimGetters.uno_get_glTexSubImage3D_ptr();
		_addresses["glCopyTexSubImage3D"] = (IntPtr)NativeShimGetters.uno_get_glCopyTexSubImage3D_ptr();
		_addresses["glCompressedTexImage3D"] = (IntPtr)NativeShimGetters.uno_get_glCompressedTexImage3D_ptr();
		_addresses["glCompressedTexSubImage2D"] = (IntPtr)NativeShimGetters.uno_get_glCompressedTexSubImage2D_ptr();
		_addresses["glCompressedTexSubImage3D"] = (IntPtr)NativeShimGetters.uno_get_glCompressedTexSubImage3D_ptr();
		_addresses["glBlitFramebuffer"] = (IntPtr)NativeShimGetters.uno_get_glBlitFramebuffer_ptr();
	}

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
	}

	// Referenced from RegisterLargeArityShimEntries behind an always-false volatile guard so the
	// IL trimmer keeps the SignaturePrimer DllImports without them ever being called.
	private static volatile bool _neverTrue = false;

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
		}
	}

	// Pointer getters for the C shims, resolved through the wasm runtime's static PInvoke table.
	// uno_gl_shim.c is linked via WasmShellNativeCompile -> NativeFileReference, and the wasm SDK
	// auto-registers every NativeFileReference's base name as a PInvoke module
	// (_WasmPInvokeModules in WasmApp.Common.targets), so DllImport("uno_gl_shim") binds
	// statically - same mechanism as DllImport("unoicu") against unoicu.a.
	private static class NativeShimGetters
	{
		private const string ShimLibrary = "uno_gl_shim";

		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_gltexImage2D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glTexSubImage2D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glTexImage3D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glTexSubImage3D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glCopyTexSubImage3D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glCompressedTexImage3D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glCompressedTexSubImage2D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glCompressedTexSubImage3D_ptr();
		[DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)] internal static extern int uno_get_glBlitFramebuffer_ptr();
	}

	// glTexSubImage2D (9 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLTexSubImage2DArgs { public int Target, Level, XOffset, YOffset, Width, Height, Format, Type, Pixels; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glTexSubImage2D_managed")]
	private static void glTexSubImage2DPacked(GLTexSubImage2DArgs* args)
		=> NativeMethods.TexSubImage2D(args->Target, args->Level, args->XOffset, args->YOffset, args->Width, args->Height, args->Format, args->Type, args->Pixels);

	// glTexImage3D (10 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLTexImage3DArgs { public int Target, Level, InternalFormat, Width, Height, Depth, Border, Format, Type, Pixels; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glTexImage3D_managed")]
	private static void glTexImage3DPacked(GLTexImage3DArgs* args)
		=> NativeMethods.TexImage3D(args->Target, args->Level, args->InternalFormat, args->Width, args->Height, args->Depth, args->Border, args->Format, args->Type, args->Pixels);

	// glTexSubImage3D (11 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLTexSubImage3DArgs { public int Target, Level, XOffset, YOffset, ZOffset, Width, Height, Depth, Format, Type, Pixels; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glTexSubImage3D_managed")]
	private static void glTexSubImage3DPacked(GLTexSubImage3DArgs* args)
		=> NativeMethods.TexSubImage3D(args->Target, args->Level, args->XOffset, args->YOffset, args->ZOffset, args->Width, args->Height, args->Depth, args->Format, args->Type, args->Pixels);

	// glCopyTexSubImage3D (9 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLCopyTexSubImage3DArgs { public int Target, Level, XOffset, YOffset, ZOffset, X, Y, Width, Height; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glCopyTexSubImage3D_managed")]
	private static void glCopyTexSubImage3DPacked(GLCopyTexSubImage3DArgs* args)
		=> NativeMethods.CopyTexSubImage3D(args->Target, args->Level, args->XOffset, args->YOffset, args->ZOffset, args->X, args->Y, args->Width, args->Height);

	// glCompressedTexImage3D (9 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLCompressedTexImage3DArgs { public int Target, Level, InternalFormat, Width, Height, Depth, Border, ImageSize, Data; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glCompressedTexImage3D_managed")]
	private static void glCompressedTexImage3DPacked(GLCompressedTexImage3DArgs* args)
		=> NativeMethods.CompressedTexImage3D(args->Target, args->Level, args->InternalFormat, args->Width, args->Height, args->Depth, args->Border, args->ImageSize, args->Data);

	// glCompressedTexSubImage2D (9 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLCompressedTexSubImage2DArgs { public int Target, Level, XOffset, YOffset, Width, Height, Format, ImageSize, Data; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glCompressedTexSubImage2D_managed")]
	private static void glCompressedTexSubImage2DPacked(GLCompressedTexSubImage2DArgs* args)
		=> NativeMethods.CompressedTexSubImage2D(args->Target, args->Level, args->XOffset, args->YOffset, args->Width, args->Height, args->Format, args->ImageSize, args->Data);

	// glCompressedTexSubImage3D (11 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLCompressedTexSubImage3DArgs { public int Target, Level, XOffset, YOffset, ZOffset, Width, Height, Depth, Format, ImageSize, Data; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glCompressedTexSubImage3D_managed")]
	private static void glCompressedTexSubImage3DPacked(GLCompressedTexSubImage3DArgs* args)
		=> NativeMethods.CompressedTexSubImage3D(args->Target, args->Level, args->XOffset, args->YOffset, args->ZOffset, args->Width, args->Height, args->Depth, args->Format, args->ImageSize, args->Data);

	// glBlitFramebuffer (10 args)
	[StructLayout(LayoutKind.Sequential)]
	private struct GLBlitFramebufferArgs { public int SrcX0, SrcY0, SrcX1, SrcY1, DstX0, DstY0, DstX1, DstY1, Mask, Filter; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glBlitFramebuffer_managed")]
	private static void glBlitFramebufferPacked(GLBlitFramebufferArgs* args)
		=> NativeMethods.BlitFramebuffer(args->SrcX0, args->SrcY0, args->SrcX1, args->SrcY1, args->DstX0, args->DstY0, args->DstX1, args->DstY1, args->Mask, args->Filter);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glTexSubImage2D")] internal static partial void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int pixelsPtr);
		[JSImport(Prefix + "glTexImage3D")] internal static partial void TexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, int pixelsPtr);
		[JSImport(Prefix + "glTexSubImage3D")] internal static partial void TexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, int pixelsPtr);
		[JSImport(Prefix + "glCopyTexSubImage3D")] internal static partial void CopyTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height);
		[JSImport(Prefix + "glCompressedTexImage3D")] internal static partial void CompressedTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, int dataPtr);
		[JSImport(Prefix + "glCompressedTexSubImage2D")] internal static partial void CompressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, int dataPtr);
		[JSImport(Prefix + "glCompressedTexSubImage3D")] internal static partial void CompressedTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, int dataPtr);
		[JSImport(Prefix + "glBlitFramebuffer")] internal static partial void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter);
	}
}
