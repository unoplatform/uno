#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Framebuffer / renderbuffer ops beyond the basic gen/bind/attach already in core.
// glBlitFramebuffer (10 args) exceeds the 8-arg UCO cap and is listed in the unsupported
// registry pending a C-shim wrapper.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterFramebufferEntries()
	{
		_addresses["glDrawBuffers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDrawBuffers;
		_addresses["glClearBufferiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glClearBufferiv;
		_addresses["glClearBufferuiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glClearBufferuiv;
		_addresses["glClearBufferfv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glClearBufferfv;
		_addresses["glClearBufferfi"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, float, int, void>)&glClearBufferfi;
		_addresses["glRenderbufferStorageMultisample"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)&glRenderbufferStorageMultisample;
		_addresses["glFramebufferTextureLayer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, uint, int, int, void>)&glFramebufferTextureLayer;
		_addresses["glInvalidateFramebuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glInvalidateFramebuffer;
		_addresses["glInvalidateSubFramebuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, int, int, int, int, void>)&glInvalidateSubFramebuffer;
		_addresses["glGetFramebufferAttachmentParameteriv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glGetFramebufferAttachmentParameteriv;
		_addresses["glGetRenderbufferParameteriv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glGetRenderbufferParameteriv;
		_addresses["glClearDepthf"] = (IntPtr)(delegate* unmanaged[Cdecl]<float, void>)&glClearDepthf;
		_addresses["glClearStencil"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glClearStencil;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDrawBuffers(int n, IntPtr bufs) => NativeMethods.DrawBuffers(n, (int)bufs);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClearBufferiv(int buffer, int drawbuffer, IntPtr value) => NativeMethods.ClearBufferiv(buffer, drawbuffer, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClearBufferuiv(int buffer, int drawbuffer, IntPtr value) => NativeMethods.ClearBufferuiv(buffer, drawbuffer, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClearBufferfv(int buffer, int drawbuffer, IntPtr value) => NativeMethods.ClearBufferfv(buffer, drawbuffer, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClearBufferfi(int buffer, int drawbuffer, float depth, int stencil) => NativeMethods.ClearBufferfi(buffer, drawbuffer, depth, stencil);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glRenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height)
		=> NativeMethods.RenderbufferStorageMultisample(target, samples, internalformat, width, height);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glFramebufferTextureLayer(int target, int attachment, uint texture, int level, int layer)
		=> NativeMethods.FramebufferTextureLayer(target, attachment, (int)texture, level, layer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glInvalidateFramebuffer(int target, int numAttachments, IntPtr attachments)
		=> NativeMethods.InvalidateFramebuffer(target, numAttachments, (int)attachments);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glInvalidateSubFramebuffer(int target, int numAttachments, IntPtr attachments, int x, int y, int width, int height)
		=> NativeMethods.InvalidateSubFramebuffer(target, numAttachments, (int)attachments, x, y, width, height);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetFramebufferAttachmentParameteriv(int target, int attachment, int pname, IntPtr @params)
		=> NativeMethods.GetFramebufferAttachmentParameteriv(target, attachment, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetRenderbufferParameteriv(int target, int pname, IntPtr @params)
		=> NativeMethods.GetRenderbufferParameteriv(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClearDepthf(float d) => NativeMethods.ClearDepthf(d);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClearStencil(int s) => NativeMethods.ClearStencil(s);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glDrawBuffers")] internal static partial void DrawBuffers(int n, int bufsPtr);
		[JSImport(Prefix + "glClearBufferiv")] internal static partial void ClearBufferiv(int buffer, int drawbuffer, int valuePtr);
		[JSImport(Prefix + "glClearBufferuiv")] internal static partial void ClearBufferuiv(int buffer, int drawbuffer, int valuePtr);
		[JSImport(Prefix + "glClearBufferfv")] internal static partial void ClearBufferfv(int buffer, int drawbuffer, int valuePtr);
		[JSImport(Prefix + "glClearBufferfi")] internal static partial void ClearBufferfi(int buffer, int drawbuffer, float depth, int stencil);
		[JSImport(Prefix + "glRenderbufferStorageMultisample")] internal static partial void RenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height);
		[JSImport(Prefix + "glFramebufferTextureLayer")] internal static partial void FramebufferTextureLayer(int target, int attachment, int texture, int level, int layer);
		[JSImport(Prefix + "glInvalidateFramebuffer")] internal static partial void InvalidateFramebuffer(int target, int numAttachments, int attachmentsPtr);
		[JSImport(Prefix + "glInvalidateSubFramebuffer")] internal static partial void InvalidateSubFramebuffer(int target, int numAttachments, int attachmentsPtr, int x, int y, int width, int height);
		[JSImport(Prefix + "glGetFramebufferAttachmentParameteriv")] internal static partial void GetFramebufferAttachmentParameteriv(int target, int attachment, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetRenderbufferParameteriv")] internal static partial void GetRenderbufferParameteriv(int target, int pname, int paramsPtr);
		[JSImport(Prefix + "glClearDepthf")] internal static partial void ClearDepthf(float d);
		[JSImport(Prefix + "glClearStencil")] internal static partial void ClearStencil(int s);
	}
}
