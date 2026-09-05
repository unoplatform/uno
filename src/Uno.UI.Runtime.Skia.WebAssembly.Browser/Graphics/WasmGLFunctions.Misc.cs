#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Miscellaneous WebGL2 functions that don't fit cleanly into the other category files:
// glGetAttachedShaders, glGetShaderPrecisionFormat, indexed parameter queries
// (Integeri_v, Integer64v, Integer64i_v, BufferParameteri64v), and indexed string query
// (glGetStringi).
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterMiscEntries()
	{
		_addresses["glGetAttachedShaders"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, IntPtr, void>)&glGetAttachedShaders;
		_addresses["glGetShaderPrecisionFormat"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, IntPtr, void>)&glGetShaderPrecisionFormat;
		_addresses["glGetIntegeri_v"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, IntPtr, void>)&glGetIntegeri_v;
		_addresses["glGetInteger64v"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGetInteger64v;
		_addresses["glGetInteger64i_v"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, IntPtr, void>)&glGetInteger64i_v;
		_addresses["glGetBufferParameteri64v"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glGetBufferParameteri64v;
		_addresses["glGetStringi"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, IntPtr>)&glGetStringi;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetAttachedShaders(uint program, int maxCount, IntPtr count, IntPtr shaders)
		=> NativeMethods.GetAttachedShaders((int)program, maxCount, (int)count, (int)shaders);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetShaderPrecisionFormat(int shadertype, int precisiontype, IntPtr range, IntPtr precision)
		=> NativeMethods.GetShaderPrecisionFormat(shadertype, precisiontype, (int)range, (int)precision);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetIntegeri_v(int target, uint index, IntPtr data)
		=> NativeMethods.GetIntegeri_v(target, (int)index, (int)data);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetInteger64v(int pname, IntPtr data)
		=> NativeMethods.GetInteger64v(pname, (int)data);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetInteger64i_v(int target, uint index, IntPtr data)
		=> NativeMethods.GetInteger64i_v(target, (int)index, (int)data);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetBufferParameteri64v(int target, int pname, IntPtr @params)
		=> NativeMethods.GetBufferParameteri64v(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static IntPtr glGetStringi(int name, uint index)
		=> (IntPtr)NativeMethods.GetStringi(name, (int)index);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glGetAttachedShaders")] internal static partial void GetAttachedShaders(int program, int maxCount, int countPtr, int shadersPtr);
		[JSImport(Prefix + "glGetShaderPrecisionFormat")] internal static partial void GetShaderPrecisionFormat(int shadertype, int precisiontype, int rangePtr, int precisionPtr);
		[JSImport(Prefix + "glGetIntegeri_v")] internal static partial void GetIntegeri_v(int target, int index, int dataPtr);
		[JSImport(Prefix + "glGetInteger64v")] internal static partial void GetInteger64v(int pname, int dataPtr);
		[JSImport(Prefix + "glGetInteger64i_v")] internal static partial void GetInteger64i_v(int target, int index, int dataPtr);
		[JSImport(Prefix + "glGetBufferParameteri64v")] internal static partial void GetBufferParameteri64v(int target, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetStringi")] internal static partial int GetStringi(int name, int index);
	}
}
