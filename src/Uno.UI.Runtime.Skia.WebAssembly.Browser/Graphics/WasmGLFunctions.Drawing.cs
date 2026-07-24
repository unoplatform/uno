#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Drawing variants: instanced and range-restricted draw calls. glDrawArrays / glDrawElements
// are already in core. glDrawArraysInstancedBaseInstance / glDrawElementsInstancedBaseInstance
// are NOT in WebGL2 - they're listed in the unsupported registry.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterDrawingEntries()
	{
		_addresses["glDrawArraysInstanced"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glDrawArraysInstanced;
		_addresses["glDrawElementsInstanced"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, int, void>)&glDrawElementsInstanced;
		_addresses["glDrawRangeElements"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, uint, int, int, IntPtr, void>)&glDrawRangeElements;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDrawArraysInstanced(int mode, int first, int count, int instanceCount)
		=> NativeMethods.DrawArraysInstanced(mode, first, count, instanceCount);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDrawElementsInstanced(int mode, int count, int type, IntPtr indices, int instanceCount)
		=> NativeMethods.DrawElementsInstanced(mode, count, type, (int)indices, instanceCount);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDrawRangeElements(int mode, uint start, uint end, int count, int type, IntPtr indices)
		=> NativeMethods.DrawRangeElements(mode, (int)start, (int)end, count, type, (int)indices);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glDrawArraysInstanced")] internal static partial void DrawArraysInstanced(int mode, int first, int count, int instanceCount);
		[JSImport(Prefix + "glDrawElementsInstanced")] internal static partial void DrawElementsInstanced(int mode, int count, int type, int indicesPtr, int instanceCount);
		[JSImport(Prefix + "glDrawRangeElements")] internal static partial void DrawRangeElements(int mode, int start, int end, int count, int type, int indicesPtr);
	}
}
