#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// Per-fragment / framebuffer state ops: blend, depth, stencil, cull, scissor, polygon offset,
// color mask, line width, hint, finish/flush, sample coverage, is* queries.
//
// GLboolean params (`normalized`, `transpose`, `red`, `green`, etc.) are declared as `int` in
// UCO signatures so the trampoline cookie stays purely 'I'-typed; the JS shim treats 0 as false.
//
// Bool return values (isXxx) are surfaced as `int` for the same reason.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterPerFragmentEntries()
	{
		_addresses["glBlendFunc"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, void>)&glBlendFunc;
		_addresses["glBlendFuncSeparate"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glBlendFuncSeparate;
		_addresses["glBlendEquation"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glBlendEquation;
		_addresses["glBlendEquationSeparate"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, void>)&glBlendEquationSeparate;
		_addresses["glBlendColor"] = (IntPtr)(delegate* unmanaged[Cdecl]<float, float, float, float, void>)&glBlendColor;

		_addresses["glDepthFunc"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glDepthFunc;
		_addresses["glDepthMask"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glDepthMask;
		_addresses["glDepthRangef"] = (IntPtr)(delegate* unmanaged[Cdecl]<float, float, void>)&glDepthRangef;
		// Alias for the desktop-GL double-precision name, which is what Silk.NET's
		// gl.DepthRange(double, double) resolves. The narrowing matches the ES semantics.
		_addresses["glDepthRange"] = (IntPtr)(delegate* unmanaged[Cdecl]<double, double, void>)&glDepthRange;

		_addresses["glStencilFunc"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, uint, void>)&glStencilFunc;
		_addresses["glStencilFuncSeparate"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, uint, void>)&glStencilFuncSeparate;
		_addresses["glStencilMask"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glStencilMask;
		_addresses["glStencilMaskSeparate"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glStencilMaskSeparate;
		_addresses["glStencilOp"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, void>)&glStencilOp;
		_addresses["glStencilOpSeparate"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glStencilOpSeparate;

		_addresses["glCullFace"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glCullFace;
		_addresses["glFrontFace"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glFrontFace;

		_addresses["glScissor"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glScissor;
		_addresses["glPolygonOffset"] = (IntPtr)(delegate* unmanaged[Cdecl]<float, float, void>)&glPolygonOffset;

		_addresses["glColorMask"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glColorMask;
		_addresses["glLineWidth"] = (IntPtr)(delegate* unmanaged[Cdecl]<float, void>)&glLineWidth;
		_addresses["glHint"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, void>)&glHint;
		_addresses["glFinish"] = (IntPtr)(delegate* unmanaged[Cdecl]<void>)&glFinish;
		_addresses["glFlush"] = (IntPtr)(delegate* unmanaged[Cdecl]<void>)&glFlush;
		_addresses["glSampleCoverage"] = (IntPtr)(delegate* unmanaged[Cdecl]<float, int, void>)&glSampleCoverage;

		_addresses["glIsEnabled"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int>)&glIsEnabled;
		_addresses["glIsBuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsBuffer;
		_addresses["glIsFramebuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsFramebuffer;
		_addresses["glIsProgram"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsProgram;
		_addresses["glIsRenderbuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsRenderbuffer;
		_addresses["glIsShader"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsShader;
		_addresses["glIsTexture"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsTexture;
		_addresses["glIsVertexArray"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsVertexArray;

		_addresses["glGetBooleanv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGetBooleanv;
		_addresses["glGetFloatv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGetFloatv;
	}

	// ---- UCO methods ----

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBlendFunc(int sfactor, int dfactor) => NativeMethods.BlendFunc(sfactor, dfactor);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBlendFuncSeparate(int srcRGB, int dstRGB, int srcAlpha, int dstAlpha)
		=> NativeMethods.BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBlendEquation(int mode) => NativeMethods.BlendEquation(mode);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBlendEquationSeparate(int modeRGB, int modeAlpha) => NativeMethods.BlendEquationSeparate(modeRGB, modeAlpha);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBlendColor(float r, float g, float b, float a) => NativeMethods.BlendColor(r, g, b, a);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDepthFunc(int func) => NativeMethods.DepthFunc(func);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDepthMask(int flag) => NativeMethods.DepthMask(flag);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDepthRangef(float n, float f) => NativeMethods.DepthRangef(n, f);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDepthRange(double n, double f) => NativeMethods.DepthRangef((float)n, (float)f);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glStencilFunc(int func, int @ref, uint mask) => NativeMethods.StencilFunc(func, @ref, (int)mask);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glStencilFuncSeparate(int face, int func, int @ref, uint mask)
		=> NativeMethods.StencilFuncSeparate(face, func, @ref, (int)mask);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glStencilMask(uint mask) => NativeMethods.StencilMask((int)mask);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glStencilMaskSeparate(int face, uint mask) => NativeMethods.StencilMaskSeparate(face, (int)mask);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glStencilOp(int fail, int zfail, int zpass) => NativeMethods.StencilOp(fail, zfail, zpass);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glStencilOpSeparate(int face, int sfail, int dpfail, int dppass)
		=> NativeMethods.StencilOpSeparate(face, sfail, dpfail, dppass);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glCullFace(int mode) => NativeMethods.CullFace(mode);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glFrontFace(int mode) => NativeMethods.FrontFace(mode);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glScissor(int x, int y, int width, int height) => NativeMethods.Scissor(x, y, width, height);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glPolygonOffset(float factor, float units) => NativeMethods.PolygonOffset(factor, units);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glColorMask(int red, int green, int blue, int alpha) => NativeMethods.ColorMask(red, green, blue, alpha);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glLineWidth(float width) => NativeMethods.LineWidth(width);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glHint(int target, int mode) => NativeMethods.Hint(target, mode);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glFinish() => NativeMethods.Finish();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glFlush() => NativeMethods.Flush();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glSampleCoverage(float value, int invert) => NativeMethods.SampleCoverage(value, invert);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsEnabled(int cap) => NativeMethods.IsEnabled(cap);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsBuffer(uint buffer) => NativeMethods.IsBuffer((int)buffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsFramebuffer(uint framebuffer) => NativeMethods.IsFramebuffer((int)framebuffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsProgram(uint program) => NativeMethods.IsProgram((int)program);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsRenderbuffer(uint renderbuffer) => NativeMethods.IsRenderbuffer((int)renderbuffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsShader(uint shader) => NativeMethods.IsShader((int)shader);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsTexture(uint texture) => NativeMethods.IsTexture((int)texture);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsVertexArray(uint array) => NativeMethods.IsVertexArray((int)array);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetBooleanv(int pname, IntPtr data) => NativeMethods.GetBooleanv(pname, (int)data);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetFloatv(int pname, IntPtr data) => NativeMethods.GetFloatv(pname, (int)data);

	// ---- JS bridges ----

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glBlendFunc")]
		internal static partial void BlendFunc(int sfactor, int dfactor);

		[JSImport(Prefix + "glBlendFuncSeparate")]
		internal static partial void BlendFuncSeparate(int srcRGB, int dstRGB, int srcAlpha, int dstAlpha);

		[JSImport(Prefix + "glBlendEquation")]
		internal static partial void BlendEquation(int mode);

		[JSImport(Prefix + "glBlendEquationSeparate")]
		internal static partial void BlendEquationSeparate(int modeRGB, int modeAlpha);

		[JSImport(Prefix + "glBlendColor")]
		internal static partial void BlendColor(float r, float g, float b, float a);

		[JSImport(Prefix + "glDepthFunc")]
		internal static partial void DepthFunc(int func);

		[JSImport(Prefix + "glDepthMask")]
		internal static partial void DepthMask(int flag);

		[JSImport(Prefix + "glDepthRangef")]
		internal static partial void DepthRangef(float n, float f);

		[JSImport(Prefix + "glStencilFunc")]
		internal static partial void StencilFunc(int func, int @ref, int mask);

		[JSImport(Prefix + "glStencilFuncSeparate")]
		internal static partial void StencilFuncSeparate(int face, int func, int @ref, int mask);

		[JSImport(Prefix + "glStencilMask")]
		internal static partial void StencilMask(int mask);

		[JSImport(Prefix + "glStencilMaskSeparate")]
		internal static partial void StencilMaskSeparate(int face, int mask);

		[JSImport(Prefix + "glStencilOp")]
		internal static partial void StencilOp(int fail, int zfail, int zpass);

		[JSImport(Prefix + "glStencilOpSeparate")]
		internal static partial void StencilOpSeparate(int face, int sfail, int dpfail, int dppass);

		[JSImport(Prefix + "glCullFace")]
		internal static partial void CullFace(int mode);

		[JSImport(Prefix + "glFrontFace")]
		internal static partial void FrontFace(int mode);

		[JSImport(Prefix + "glScissor")]
		internal static partial void Scissor(int x, int y, int width, int height);

		[JSImport(Prefix + "glPolygonOffset")]
		internal static partial void PolygonOffset(float factor, float units);

		[JSImport(Prefix + "glColorMask")]
		internal static partial void ColorMask(int red, int green, int blue, int alpha);

		[JSImport(Prefix + "glLineWidth")]
		internal static partial void LineWidth(float width);

		[JSImport(Prefix + "glHint")]
		internal static partial void Hint(int target, int mode);

		[JSImport(Prefix + "glFinish")]
		internal static partial void Finish();

		[JSImport(Prefix + "glFlush")]
		internal static partial void Flush();

		[JSImport(Prefix + "glSampleCoverage")]
		internal static partial void SampleCoverage(float value, int invert);

		[JSImport(Prefix + "glIsEnabled")]
		internal static partial int IsEnabled(int cap);

		[JSImport(Prefix + "glIsBuffer")]
		internal static partial int IsBuffer(int buffer);

		[JSImport(Prefix + "glIsFramebuffer")]
		internal static partial int IsFramebuffer(int framebuffer);

		[JSImport(Prefix + "glIsProgram")]
		internal static partial int IsProgram(int program);

		[JSImport(Prefix + "glIsRenderbuffer")]
		internal static partial int IsRenderbuffer(int renderbuffer);

		[JSImport(Prefix + "glIsShader")]
		internal static partial int IsShader(int shader);

		[JSImport(Prefix + "glIsTexture")]
		internal static partial int IsTexture(int texture);

		[JSImport(Prefix + "glIsVertexArray")]
		internal static partial int IsVertexArray(int array);

		[JSImport(Prefix + "glGetBooleanv")]
		internal static partial void GetBooleanv(int pname, int dataPtr);

		[JSImport(Prefix + "glGetFloatv")]
		internal static partial void GetFloatv(int pname, int dataPtr);
	}
}
