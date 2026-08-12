#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// WebGL2 advanced object types: samplers, sync, queries, transform feedback.
//
// Sync objects use GLuint64 timeouts which we surface as a managed `long` (i64). Mono-wasm
// passes long through as i64 in calli, cookie char 'L' (mapped by mono's type_to_c).
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterAdvancedFeaturesEntries()
	{
		// Samplers
		_addresses["glGenSamplers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenSamplers;
		_addresses["glDeleteSamplers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteSamplers;
		_addresses["glIsSampler"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsSampler;
		_addresses["glBindSampler"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, void>)&glBindSampler;
		_addresses["glSamplerParameteri"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, int, void>)&glSamplerParameteri;
		_addresses["glSamplerParameterf"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, float, void>)&glSamplerParameterf;
		_addresses["glSamplerParameteriv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glSamplerParameteriv;
		_addresses["glSamplerParameterfv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glSamplerParameterfv;
		_addresses["glGetSamplerParameteriv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetSamplerParameteriv;
		_addresses["glGetSamplerParameterfv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetSamplerParameterfv;

		// Queries
		_addresses["glGenQueries"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenQueries;
		_addresses["glDeleteQueries"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteQueries;
		_addresses["glIsQuery"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsQuery;
		_addresses["glBeginQuery"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glBeginQuery;
		_addresses["glEndQuery"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glEndQuery;
		_addresses["glGetQueryiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glGetQueryiv;
		_addresses["glGetQueryObjectuiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetQueryObjectuiv;

		// Sync. fenceSync returns a GLsync (opaque pointer-like, we surface as int).
		_addresses["glFenceSync"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, IntPtr>)&glFenceSync;
		_addresses["glIsSync"] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int>)&glIsSync;
		_addresses["glDeleteSync"] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void>)&glDeleteSync;
		_addresses["glClientWaitSync"] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, uint, ulong, int>)&glClientWaitSync;
		_addresses["glWaitSync"] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, uint, ulong, void>)&glWaitSync;
		_addresses["glGetSynciv"] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, int, IntPtr, IntPtr, void>)&glGetSynciv;

		// Transform feedback
		_addresses["glGenTransformFeedbacks"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenTransformFeedbacks;
		_addresses["glDeleteTransformFeedbacks"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteTransformFeedbacks;
		_addresses["glIsTransformFeedback"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int>)&glIsTransformFeedback;
		_addresses["glBindTransformFeedback"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glBindTransformFeedback;
		_addresses["glBeginTransformFeedback"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glBeginTransformFeedback;
		_addresses["glEndTransformFeedback"] = (IntPtr)(delegate* unmanaged[Cdecl]<void>)&glEndTransformFeedback;
		_addresses["glPauseTransformFeedback"] = (IntPtr)(delegate* unmanaged[Cdecl]<void>)&glPauseTransformFeedback;
		_addresses["glResumeTransformFeedback"] = (IntPtr)(delegate* unmanaged[Cdecl]<void>)&glResumeTransformFeedback;
		_addresses["glTransformFeedbackVaryings"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, int, void>)&glTransformFeedbackVaryings;
		_addresses["glGetTransformFeedbackVarying"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, IntPtr, IntPtr, IntPtr, void>)&glGetTransformFeedbackVarying;
	}

	// Samplers
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenSamplers(int count, IntPtr samplers) => NativeMethods.GenSamplers(count, (int)samplers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteSamplers(int count, IntPtr samplers) => NativeMethods.DeleteSamplers(count, (int)samplers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsSampler(uint sampler) => NativeMethods.IsSampler((int)sampler);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindSampler(uint unit, uint sampler) => NativeMethods.BindSampler((int)unit, (int)sampler);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glSamplerParameteri(uint sampler, int pname, int param) => NativeMethods.SamplerParameteri((int)sampler, pname, param);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glSamplerParameterf(uint sampler, int pname, float param) => NativeMethods.SamplerParameterf((int)sampler, pname, param);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glSamplerParameteriv(uint sampler, int pname, IntPtr @params) => NativeMethods.SamplerParameteriv((int)sampler, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glSamplerParameterfv(uint sampler, int pname, IntPtr @params) => NativeMethods.SamplerParameterfv((int)sampler, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetSamplerParameteriv(uint sampler, int pname, IntPtr @params) => NativeMethods.GetSamplerParameteriv((int)sampler, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetSamplerParameterfv(uint sampler, int pname, IntPtr @params) => NativeMethods.GetSamplerParameterfv((int)sampler, pname, (int)@params);

	// Queries
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenQueries(int n, IntPtr ids) => NativeMethods.GenQueries(n, (int)ids);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteQueries(int n, IntPtr ids) => NativeMethods.DeleteQueries(n, (int)ids);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsQuery(uint id) => NativeMethods.IsQuery((int)id);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBeginQuery(int target, uint id) => NativeMethods.BeginQuery(target, (int)id);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glEndQuery(int target) => NativeMethods.EndQuery(target);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetQueryiv(int target, int pname, IntPtr @params) => NativeMethods.GetQueryiv(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetQueryObjectuiv(uint id, int pname, IntPtr @params) => NativeMethods.GetQueryObjectuiv((int)id, pname, (int)@params);

	// Sync
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static IntPtr glFenceSync(int condition, uint flags) => (IntPtr)NativeMethods.FenceSync(condition, (int)flags);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsSync(IntPtr sync) => NativeMethods.IsSync((int)sync);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteSync(IntPtr sync) => NativeMethods.DeleteSync((int)sync);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glClientWaitSync(IntPtr sync, uint flags, ulong timeout) => NativeMethods.ClientWaitSync((int)sync, (int)flags, (long)timeout);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glWaitSync(IntPtr sync, uint flags, ulong timeout) => NativeMethods.WaitSync((int)sync, (int)flags, (long)timeout);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetSynciv(IntPtr sync, int pname, int bufSize, IntPtr length, IntPtr values)
		=> NativeMethods.GetSynciv((int)sync, pname, bufSize, (int)length, (int)values);

	// Transform feedback
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenTransformFeedbacks(int n, IntPtr ids) => NativeMethods.GenTransformFeedbacks(n, (int)ids);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteTransformFeedbacks(int n, IntPtr ids) => NativeMethods.DeleteTransformFeedbacks(n, (int)ids);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glIsTransformFeedback(uint id) => NativeMethods.IsTransformFeedback((int)id);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindTransformFeedback(int target, uint id) => NativeMethods.BindTransformFeedback(target, (int)id);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBeginTransformFeedback(int primitiveMode) => NativeMethods.BeginTransformFeedback(primitiveMode);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glEndTransformFeedback() => NativeMethods.EndTransformFeedback();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glPauseTransformFeedback() => NativeMethods.PauseTransformFeedback();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glResumeTransformFeedback() => NativeMethods.ResumeTransformFeedback();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTransformFeedbackVaryings(uint program, int count, IntPtr varyings, int bufferMode)
		=> NativeMethods.TransformFeedbackVaryings((int)program, count, (int)varyings, bufferMode);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetTransformFeedbackVarying(uint program, uint index, int bufSize, IntPtr length, IntPtr size, IntPtr type, IntPtr name)
		=> NativeMethods.GetTransformFeedbackVarying((int)program, (int)index, bufSize, (int)length, (int)size, (int)type, (int)name);

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glGenSamplers")] internal static partial void GenSamplers(int count, int samplersPtr);
		[JSImport(Prefix + "glDeleteSamplers")] internal static partial void DeleteSamplers(int count, int samplersPtr);
		[JSImport(Prefix + "glIsSampler")] internal static partial int IsSampler(int sampler);
		[JSImport(Prefix + "glBindSampler")] internal static partial void BindSampler(int unit, int sampler);
		[JSImport(Prefix + "glSamplerParameteri")] internal static partial void SamplerParameteri(int sampler, int pname, int param);
		[JSImport(Prefix + "glSamplerParameterf")] internal static partial void SamplerParameterf(int sampler, int pname, float param);
		[JSImport(Prefix + "glSamplerParameteriv")] internal static partial void SamplerParameteriv(int sampler, int pname, int paramsPtr);
		[JSImport(Prefix + "glSamplerParameterfv")] internal static partial void SamplerParameterfv(int sampler, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetSamplerParameteriv")] internal static partial void GetSamplerParameteriv(int sampler, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetSamplerParameterfv")] internal static partial void GetSamplerParameterfv(int sampler, int pname, int paramsPtr);

		[JSImport(Prefix + "glGenQueries")] internal static partial void GenQueries(int n, int idsPtr);
		[JSImport(Prefix + "glDeleteQueries")] internal static partial void DeleteQueries(int n, int idsPtr);
		[JSImport(Prefix + "glIsQuery")] internal static partial int IsQuery(int id);
		[JSImport(Prefix + "glBeginQuery")] internal static partial void BeginQuery(int target, int id);
		[JSImport(Prefix + "glEndQuery")] internal static partial void EndQuery(int target);
		[JSImport(Prefix + "glGetQueryiv")] internal static partial void GetQueryiv(int target, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetQueryObjectuiv")] internal static partial void GetQueryObjectuiv(int id, int pname, int paramsPtr);

		[JSImport(Prefix + "glFenceSync")] internal static partial int FenceSync(int condition, int flags);
		[JSImport(Prefix + "glIsSync")] internal static partial int IsSync(int sync);
		[JSImport(Prefix + "glDeleteSync")] internal static partial void DeleteSync(int sync);
		[JSImport(Prefix + "glClientWaitSync")] internal static partial int ClientWaitSync(int sync, int flags, [JSMarshalAs<JSType.Number>] long timeout);
		[JSImport(Prefix + "glWaitSync")] internal static partial void WaitSync(int sync, int flags, [JSMarshalAs<JSType.Number>] long timeout);
		[JSImport(Prefix + "glGetSynciv")] internal static partial void GetSynciv(int sync, int pname, int bufSize, int lengthPtr, int valuesPtr);

		[JSImport(Prefix + "glGenTransformFeedbacks")] internal static partial void GenTransformFeedbacks(int n, int idsPtr);
		[JSImport(Prefix + "glDeleteTransformFeedbacks")] internal static partial void DeleteTransformFeedbacks(int n, int idsPtr);
		[JSImport(Prefix + "glIsTransformFeedback")] internal static partial int IsTransformFeedback(int id);
		[JSImport(Prefix + "glBindTransformFeedback")] internal static partial void BindTransformFeedback(int target, int id);
		[JSImport(Prefix + "glBeginTransformFeedback")] internal static partial void BeginTransformFeedback(int primitiveMode);
		[JSImport(Prefix + "glEndTransformFeedback")] internal static partial void EndTransformFeedback();
		[JSImport(Prefix + "glPauseTransformFeedback")] internal static partial void PauseTransformFeedback();
		[JSImport(Prefix + "glResumeTransformFeedback")] internal static partial void ResumeTransformFeedback();
		[JSImport(Prefix + "glTransformFeedbackVaryings")] internal static partial void TransformFeedbackVaryings(int program, int count, int varyingsPtr, int bufferMode);
		[JSImport(Prefix + "glGetTransformFeedbackVarying")] internal static partial void GetTransformFeedbackVarying(int program, int index, int bufSize, int lengthPtr, int sizePtr, int typePtr, int namePtr);
	}
}
