#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Buffer-object operations beyond the basic create/bind/data already in core: subdata,
// copy-between-buffers (WebGL2), indexed binding (WebGL2 UBO/TF), parameter queries.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterBufferEntries()
	{
		_addresses["glBufferSubData"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glBufferSubData;
		_addresses["glCopyBufferSubData"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)&glCopyBufferSubData;
		_addresses["glBindBufferBase"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, uint, void>)&glBindBufferBase;
		_addresses["glBindBufferRange"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, uint, int, int, void>)&glBindBufferRange;
		_addresses["glGetBufferParameteriv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glGetBufferParameteriv;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBufferSubData(int target, int offset, int size, IntPtr data) => NativeMethods.BufferSubData(target, offset, size, (int)data);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glCopyBufferSubData(int readTarget, int writeTarget, int readOffset, int writeOffset, int size)
		=> NativeMethods.CopyBufferSubData(readTarget, writeTarget, readOffset, writeOffset, size);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindBufferBase(int target, uint index, uint buffer) => NativeMethods.BindBufferBase(target, (int)index, (int)buffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindBufferRange(int target, uint index, uint buffer, int offset, int size)
		=> NativeMethods.BindBufferRange(target, (int)index, (int)buffer, offset, size);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetBufferParameteriv(int target, int pname, IntPtr @params) => NativeMethods.GetBufferParameteriv(target, pname, (int)@params);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glBufferSubData")] internal static partial void BufferSubData(int target, int offset, int size, int dataPtr);
		[JSImport(Prefix + "glCopyBufferSubData")] internal static partial void CopyBufferSubData(int readTarget, int writeTarget, int readOffset, int writeOffset, int size);
		[JSImport(Prefix + "glBindBufferBase")] internal static partial void BindBufferBase(int target, int index, int buffer);
		[JSImport(Prefix + "glBindBufferRange")] internal static partial void BindBufferRange(int target, int index, int buffer, int offset, int size);
		[JSImport(Prefix + "glGetBufferParameteriv")] internal static partial void GetBufferParameteriv(int target, int pname, int paramsPtr);
	}
}
