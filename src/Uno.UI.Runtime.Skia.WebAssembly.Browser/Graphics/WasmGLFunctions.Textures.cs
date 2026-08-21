#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Texture ops beyond the basic gen/bind/image2D already in core: parameter set with float,
// mipmap generation, copies between textures, immutable storage (WebGL2), compressed uploads.
//
// >8-arg texture functions (glTexSubImage2D, glTexImage3D, glTexSubImage3D,
// glCompressedTexImage3D, glCompressedTexSubImage*, glCopyTexSubImage3D) exceed the mono-wasm
// interpreter's [UnmanagedCallersOnly] cap (dotnet/runtime#109338); they're listed in the
// unsupported registry with "needs C shim" annotations.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterTextureEntries()
	{
		_addresses["glTexParameterf"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, float, void>)&glTexParameterf;
		_addresses["glTexParameterfv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glTexParameterfv;
		_addresses["glTexParameteriv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glTexParameteriv;
		_addresses["glGenerateMipmap"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glGenerateMipmap;
		_addresses["glCopyTexImage2D"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void>)&glCopyTexImage2D;
		_addresses["glCopyTexSubImage2D"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, int, void>)&glCopyTexSubImage2D;
		_addresses["glTexStorage2D"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)&glTexStorage2D;
		_addresses["glTexStorage3D"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void>)&glTexStorage3D;
		_addresses["glCompressedTexImage2D"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, int, int, IntPtr, void>)&glCompressedTexImage2D;
		_addresses["glGetTexParameterfv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glGetTexParameterfv;
		_addresses["glGetTexParameteriv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glGetTexParameteriv;
		_addresses["glGetInternalformativ"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, IntPtr, void>)&glGetInternalformativ;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTexParameterf(int target, int pname, float param) => NativeMethods.TexParameterf(target, pname, param);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTexParameterfv(int target, int pname, IntPtr @params) => NativeMethods.TexParameterfv(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTexParameteriv(int target, int pname, IntPtr @params) => NativeMethods.TexParameteriv(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenerateMipmap(int target) => NativeMethods.GenerateMipmap(target);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glCopyTexImage2D(int target, int level, int internalformat, int x, int y, int width, int height, int border)
		=> NativeMethods.CopyTexImage2D(target, level, internalformat, x, y, width, height, border);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glCopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height)
		=> NativeMethods.CopyTexSubImage2D(target, level, xoffset, yoffset, x, y, width, height);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTexStorage2D(int target, int levels, int internalformat, int width, int height)
		=> NativeMethods.TexStorage2D(target, levels, internalformat, width, height);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTexStorage3D(int target, int levels, int internalformat, int width, int height, int depth)
		=> NativeMethods.TexStorage3D(target, levels, internalformat, width, height, depth);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glCompressedTexImage2D(int target, int level, int internalformat, int width, int height, int border, int imageSize, IntPtr data)
		=> NativeMethods.CompressedTexImage2D(target, level, internalformat, width, height, border, imageSize, (int)data);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetTexParameterfv(int target, int pname, IntPtr @params) => NativeMethods.GetTexParameterfv(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetTexParameteriv(int target, int pname, IntPtr @params) => NativeMethods.GetTexParameteriv(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetInternalformativ(int target, int internalformat, int pname, int bufSize, IntPtr @params)
		=> NativeMethods.GetInternalformativ(target, internalformat, pname, bufSize, (int)@params);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glTexParameterf")] internal static partial void TexParameterf(int target, int pname, float param);
		[JSImport(Prefix + "glTexParameterfv")] internal static partial void TexParameterfv(int target, int pname, int paramsPtr);
		[JSImport(Prefix + "glTexParameteriv")] internal static partial void TexParameteriv(int target, int pname, int paramsPtr);
		[JSImport(Prefix + "glGenerateMipmap")] internal static partial void GenerateMipmap(int target);
		[JSImport(Prefix + "glCopyTexImage2D")] internal static partial void CopyTexImage2D(int target, int level, int internalformat, int x, int y, int width, int height, int border);
		[JSImport(Prefix + "glCopyTexSubImage2D")] internal static partial void CopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height);
		[JSImport(Prefix + "glTexStorage2D")] internal static partial void TexStorage2D(int target, int levels, int internalformat, int width, int height);
		[JSImport(Prefix + "glTexStorage3D")] internal static partial void TexStorage3D(int target, int levels, int internalformat, int width, int height, int depth);
		[JSImport(Prefix + "glCompressedTexImage2D")] internal static partial void CompressedTexImage2D(int target, int level, int internalformat, int width, int height, int border, int imageSize, int dataPtr);
		[JSImport(Prefix + "glGetTexParameterfv")] internal static partial void GetTexParameterfv(int target, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetTexParameteriv")] internal static partial void GetTexParameteriv(int target, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetInternalformativ")] internal static partial void GetInternalformativ(int target, int internalformat, int pname, int bufSize, int paramsPtr);
	}
}
