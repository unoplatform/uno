#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Vertex attribute set/get variants and instancing-related per-attrib ops.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterVertexArrayEntries()
	{
		_addresses["glVertexAttrib1f"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, float, void>)&glVertexAttrib1f;
		_addresses["glVertexAttrib2f"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, float, float, void>)&glVertexAttrib2f;
		_addresses["glVertexAttrib3f"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, float, float, float, void>)&glVertexAttrib3f;
		_addresses["glVertexAttrib4f"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, float, float, float, float, void>)&glVertexAttrib4f;

		_addresses["glVertexAttrib1fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, void>)&glVertexAttrib1fv;
		_addresses["glVertexAttrib2fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, void>)&glVertexAttrib2fv;
		_addresses["glVertexAttrib3fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, void>)&glVertexAttrib3fv;
		_addresses["glVertexAttrib4fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, void>)&glVertexAttrib4fv;

		_addresses["glVertexAttribI4i"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, int, int, int, void>)&glVertexAttribI4i;
		_addresses["glVertexAttribI4ui"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, uint, uint, uint, void>)&glVertexAttribI4ui;
		_addresses["glVertexAttribI4iv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, void>)&glVertexAttribI4iv;
		_addresses["glVertexAttribI4uiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, void>)&glVertexAttribI4uiv;

		_addresses["glVertexAttribIPointer"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, int, int, IntPtr, void>)&glVertexAttribIPointer;
		_addresses["glVertexAttribDivisor"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, void>)&glVertexAttribDivisor;

		_addresses["glGetVertexAttribfv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetVertexAttribfv;
		_addresses["glGetVertexAttribiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetVertexAttribiv;
		_addresses["glGetVertexAttribIiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetVertexAttribIiv;
		_addresses["glGetVertexAttribIuiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetVertexAttribIuiv;
		_addresses["glGetVertexAttribPointerv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetVertexAttribPointerv;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib1f(uint index, float v0) => NativeMethods.VertexAttrib1f((int)index, v0);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib2f(uint index, float v0, float v1) => NativeMethods.VertexAttrib2f((int)index, v0, v1);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib3f(uint index, float v0, float v1, float v2) => NativeMethods.VertexAttrib3f((int)index, v0, v1, v2);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib4f(uint index, float v0, float v1, float v2, float v3) => NativeMethods.VertexAttrib4f((int)index, v0, v1, v2, v3);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib1fv(uint index, IntPtr v) => NativeMethods.VertexAttrib1fv((int)index, (int)v);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib2fv(uint index, IntPtr v) => NativeMethods.VertexAttrib2fv((int)index, (int)v);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib3fv(uint index, IntPtr v) => NativeMethods.VertexAttrib3fv((int)index, (int)v);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttrib4fv(uint index, IntPtr v) => NativeMethods.VertexAttrib4fv((int)index, (int)v);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttribI4i(uint index, int x, int y, int z, int w) => NativeMethods.VertexAttribI4i((int)index, x, y, z, w);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttribI4ui(uint index, uint x, uint y, uint z, uint w) => NativeMethods.VertexAttribI4ui((int)index, (int)x, (int)y, (int)z, (int)w);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttribI4iv(uint index, IntPtr v) => NativeMethods.VertexAttribI4iv((int)index, (int)v);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttribI4uiv(uint index, IntPtr v) => NativeMethods.VertexAttribI4uiv((int)index, (int)v);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttribIPointer(uint index, int size, int type, int stride, IntPtr pointer)
		=> NativeMethods.VertexAttribIPointer((int)index, size, type, stride, (int)pointer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttribDivisor(uint index, uint divisor) => NativeMethods.VertexAttribDivisor((int)index, (int)divisor);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetVertexAttribfv(uint index, int pname, IntPtr @params) => NativeMethods.GetVertexAttribfv((int)index, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetVertexAttribiv(uint index, int pname, IntPtr @params) => NativeMethods.GetVertexAttribiv((int)index, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetVertexAttribIiv(uint index, int pname, IntPtr @params) => NativeMethods.GetVertexAttribIiv((int)index, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetVertexAttribIuiv(uint index, int pname, IntPtr @params) => NativeMethods.GetVertexAttribIuiv((int)index, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetVertexAttribPointerv(uint index, int pname, IntPtr pointer) => NativeMethods.GetVertexAttribPointerv((int)index, pname, (int)pointer);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glVertexAttrib1f")] internal static partial void VertexAttrib1f(int index, float v0);
		[JSImport(Prefix + "glVertexAttrib2f")] internal static partial void VertexAttrib2f(int index, float v0, float v1);
		[JSImport(Prefix + "glVertexAttrib3f")] internal static partial void VertexAttrib3f(int index, float v0, float v1, float v2);
		[JSImport(Prefix + "glVertexAttrib4f")] internal static partial void VertexAttrib4f(int index, float v0, float v1, float v2, float v3);
		[JSImport(Prefix + "glVertexAttrib1fv")] internal static partial void VertexAttrib1fv(int index, int vPtr);
		[JSImport(Prefix + "glVertexAttrib2fv")] internal static partial void VertexAttrib2fv(int index, int vPtr);
		[JSImport(Prefix + "glVertexAttrib3fv")] internal static partial void VertexAttrib3fv(int index, int vPtr);
		[JSImport(Prefix + "glVertexAttrib4fv")] internal static partial void VertexAttrib4fv(int index, int vPtr);
		[JSImport(Prefix + "glVertexAttribI4i")] internal static partial void VertexAttribI4i(int index, int x, int y, int z, int w);
		[JSImport(Prefix + "glVertexAttribI4ui")] internal static partial void VertexAttribI4ui(int index, int x, int y, int z, int w);
		[JSImport(Prefix + "glVertexAttribI4iv")] internal static partial void VertexAttribI4iv(int index, int vPtr);
		[JSImport(Prefix + "glVertexAttribI4uiv")] internal static partial void VertexAttribI4uiv(int index, int vPtr);
		[JSImport(Prefix + "glVertexAttribIPointer")] internal static partial void VertexAttribIPointer(int index, int size, int type, int stride, int pointer);
		[JSImport(Prefix + "glVertexAttribDivisor")] internal static partial void VertexAttribDivisor(int index, int divisor);
		[JSImport(Prefix + "glGetVertexAttribfv")] internal static partial void GetVertexAttribfv(int index, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetVertexAttribiv")] internal static partial void GetVertexAttribiv(int index, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetVertexAttribIiv")] internal static partial void GetVertexAttribIiv(int index, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetVertexAttribIuiv")] internal static partial void GetVertexAttribIuiv(int index, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetVertexAttribPointerv")] internal static partial void GetVertexAttribPointerv(int index, int pname, int pointerPtr);
	}
}
