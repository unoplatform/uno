#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// JS-backed implementations of OpenGL ES 3.0 entry points used by GLCanvasElement on browser-wasm.
// Each entry is an [UnmanagedCallersOnly] static method whose address lives in dotnet.wasm's own
// function table; Silk.NET resolves them via INativeContext.GetProcAddress and calls them as
// delegate*[unmanaged[Cdecl]]. The bodies trampoline into the TS WebGL2 shim through [JSImport].
//
// The JSImport source generator doesn't support uint/ushort, so the NativeMethods bridge passes
// everything as int; uint args from the [UnmanagedCallersOnly] signature are bit-cast at the
// call boundary (no behavior change since wasm32 transfers them as i32 anyway).
//
// Entries with 9 or more arguments (glTexImage2D in v1) can't be dispatched directly from managed
// code: the mono-wasm interpreter caps native-to-interp trampolines at 8 args (dotnet/runtime#109338).
// These route through a native C shim (build/native/uno_gl_shim.c) that packs args into a struct
// and calls a 1-arg managed dispatcher.
//
// Float-bearing calli signatures (VFFFF for gl.ClearColor, VIF for gl.Uniform1f, VIFFFF for
// gl.Uniform4f) need to be discovered by the build-time PInvokeTableGenerator via [DllImport]
// declarations in the head assembly; see UnoGLCanvasElementWasmSignaturePrimer.cs which is
// Compile-included into the consuming app by this framework's .targets file.
//
// Adding a new entry: add an [UnmanagedCallersOnly] method below, register it in the static ctor
// dictionary, and add the matching JSImport + TS implementation. If the new signature has any
// float/double parameters, add a matching [DllImport] dummy to the primer too.
internal static unsafe partial class WasmGLFunctions
{
	private static Dictionary<string, IntPtr> _addresses = null!;

	public static IntPtr GetProcAddress(string name)
	{
		if (_addresses.TryGetValue(name, out var p))
		{
			return p;
		}

		if (_unsupportedOnWebGL2.TryGetValue(name, out var reason))
		{
			typeof(WasmGLFunctions).Log().Error(
				$"GL function '{name}' is not available on WebGL2: {reason}. " +
				$"Silk.NET will report 'No function was found'. Consider feature-detecting before calling.");
		}

		return IntPtr.Zero;
	}

	static WasmGLFunctions()
	{
		RegisterCoreEntries();
		RegisterPerFragmentEntries();
		RegisterUniformEntries();
		RegisterBufferEntries();
		RegisterVertexArrayEntries();
		RegisterTextureEntries();
		RegisterFramebufferEntries();
		RegisterDrawingEntries();
		RegisterAdvancedFeaturesEntries();
		RegisterLargeArityShimEntries();
		RegisterMiscEntries();
	}

	private static unsafe void RegisterCoreEntries()
	{
		_addresses = new(StringComparer.Ordinal)
		{
			// State / queries
			["glGetString"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr>)&glGetString,
			["glGetIntegerv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGetIntegerv,
			["glGetError"] = (IntPtr)(delegate* unmanaged[Cdecl]<int>)&glGetError,
			["glViewport"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glViewport,
			["glEnable"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glEnable,
			["glDisable"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glDisable,
			["glClear"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glClear,
			["glClearColor"] = (IntPtr)(delegate* unmanaged[Cdecl]<float, float, float, float, void>)&glClearColor,
			["glDrawArrays"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, void>)&glDrawArrays,
			["glDrawElements"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glDrawElements,
			["glPixelStorei"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, void>)&glPixelStorei,

			// Buffers
			["glGenBuffers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenBuffers,
			["glDeleteBuffers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteBuffers,
			["glBindBuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glBindBuffer,
			["glBufferData"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, int, void>)&glBufferData,

			// Vertex arrays
			["glGenVertexArrays"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenVertexArrays,
			["glDeleteVertexArrays"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteVertexArrays,
			["glBindVertexArray"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glBindVertexArray,
			["glVertexAttribPointer"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, int, int, int, IntPtr, void>)&glVertexAttribPointer,
			["glEnableVertexAttribArray"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glEnableVertexAttribArray,
			["glDisableVertexAttribArray"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glDisableVertexAttribArray,

			// Textures
			["glGenTextures"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenTextures,
			["glDeleteTextures"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteTextures,
			["glBindTexture"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glBindTexture,
			["glActiveTexture"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glActiveTexture,
			["glTexImage2D"] = GetShimPtr("uno_get_gltexImage2D_ptr"),
			["glTexParameteri"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, void>)&glTexParameteri,
			["glTexParameterIuiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glTexParameterIuiv,
			["glReadBuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&glReadBuffer,
			["glReadPixels"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, int, IntPtr, void>)&glReadPixels,

			// Framebuffers / renderbuffers
			["glGenFramebuffers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenFramebuffers,
			["glDeleteFramebuffers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteFramebuffers,
			["glBindFramebuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glBindFramebuffer,
			["glCheckFramebufferStatus"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int>)&glCheckFramebufferStatus,
			["glFramebufferTexture2D"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, uint, int, void>)&glFramebufferTexture2D,
			["glFramebufferRenderbuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, uint, void>)&glFramebufferRenderbuffer,
			["glGenRenderbuffers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glGenRenderbuffers,
			["glDeleteRenderbuffers"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, IntPtr, void>)&glDeleteRenderbuffers,
			["glBindRenderbuffer"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glBindRenderbuffer,
			["glRenderbufferStorage"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glRenderbufferStorage,

			// Shaders / programs
			["glCreateShader"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint>)&glCreateShader,
			["glDeleteShader"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glDeleteShader,
			["glShaderSource"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, IntPtr, void>)&glShaderSource,
			["glCompileShader"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glCompileShader,
			["glGetShaderiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetShaderiv,
			["glGetShaderInfoLog"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, IntPtr, void>)&glGetShaderInfoLog,
			["glCreateProgram"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint>)&glCreateProgram,
			["glDeleteProgram"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glDeleteProgram,
			["glAttachShader"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, void>)&glAttachShader,
			["glDetachShader"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, void>)&glDetachShader,
			["glLinkProgram"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glLinkProgram,
			["glGetProgramiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetProgramiv,
			["glGetProgramInfoLog"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, IntPtr, void>)&glGetProgramInfoLog,
			["glUseProgram"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glUseProgram,
			["glGetUniformLocation"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, int>)&glGetUniformLocation,
			["glUniform1i"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, void>)&glUniform1i,
			["glUniform1f"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, float, void>)&glUniform1f,
			["glUniform4f"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, float, float, float, float, void>)&glUniform4f,
		};
	}

	// ----------------------------------------------------------------------------------------
	// State / queries
	// ----------------------------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static IntPtr glGetString(int name) => (IntPtr)NativeMethods.GetString(name);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetIntegerv(int pname, IntPtr data) => NativeMethods.GetIntegerv(pname, (int)data);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glGetError() => NativeMethods.GetError();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glViewport(int x, int y, int width, int height) => NativeMethods.Viewport(x, y, width, height);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glEnable(int cap) => NativeMethods.Enable(cap);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDisable(int cap) => NativeMethods.Disable(cap);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClear(uint mask) => NativeMethods.Clear((int)mask);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glClearColor(float r, float g, float b, float a) => NativeMethods.ClearColor(r, g, b, a);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDrawArrays(int mode, int first, int count) => NativeMethods.DrawArrays(mode, first, count);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDrawElements(int mode, int count, int type, IntPtr indices) => NativeMethods.DrawElements(mode, count, type, (int)indices);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glPixelStorei(int pname, int param) => NativeMethods.PixelStorei(pname, param);

	// ----------------------------------------------------------------------------------------
	// Buffers
	// ----------------------------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenBuffers(int n, IntPtr buffers) => NativeMethods.GenBuffers(n, (int)buffers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteBuffers(int n, IntPtr buffers) => NativeMethods.DeleteBuffers(n, (int)buffers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindBuffer(int target, uint buffer) => NativeMethods.BindBuffer(target, (int)buffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBufferData(int target, int size, IntPtr data, int usage) => NativeMethods.BufferData(target, size, (int)data, usage);

	// ----------------------------------------------------------------------------------------
	// Vertex arrays
	// ----------------------------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenVertexArrays(int n, IntPtr arrays) => NativeMethods.GenVertexArrays(n, (int)arrays);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteVertexArrays(int n, IntPtr arrays) => NativeMethods.DeleteVertexArrays(n, (int)arrays);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindVertexArray(uint array) => NativeMethods.BindVertexArray((int)array);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glVertexAttribPointer(uint index, int size, int type, int normalized, int stride, IntPtr pointer)
		=> NativeMethods.VertexAttribPointer((int)index, size, type, normalized, stride, (int)pointer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glEnableVertexAttribArray(uint index) => NativeMethods.EnableVertexAttribArray((int)index);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDisableVertexAttribArray(uint index) => NativeMethods.DisableVertexAttribArray((int)index);

	// ----------------------------------------------------------------------------------------
	// Textures
	// ----------------------------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenTextures(int n, IntPtr textures) => NativeMethods.GenTextures(n, (int)textures);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteTextures(int n, IntPtr textures) => NativeMethods.DeleteTextures(n, (int)textures);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindTexture(int target, uint texture) => NativeMethods.BindTexture(target, (int)texture);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glActiveTexture(int texture) => NativeMethods.ActiveTexture(texture);

	// glTexImage2D has 9 args -> dispatched through the native C shim (uno_gl_shim.c) which packs
	// the call into a struct and calls glTexImage2DPacked below as a 1-arg [UnmanagedCallersOnly].
	[StructLayout(LayoutKind.Sequential)]
	private struct GLTexImage2DArgs
	{
		public int Target;
		public int Level;
		public int InternalFormat;
		public int Width;
		public int Height;
		public int Border;
		public int Format;
		public int Type;
		public int Pixels;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "uno_glTexImage2D_managed")]
	private static void glTexImage2DPacked(GLTexImage2DArgs* args)
		=> NativeMethods.TexImage2D(
			args->Target, args->Level, args->InternalFormat,
			args->Width, args->Height, args->Border,
			args->Format, args->Type, args->Pixels);

	// Resolve a C-shim function pointer by calling its EMSCRIPTEN_KEEPALIVE-exported getter
	// (uno_get_<name>_ptr) through JS. Used for every gl function whose argument count exceeds
	// the [UnmanagedCallersOnly] cap (dotnet/runtime#109338) and therefore has to be reached
	// via a native wrapper in uno_gl_shim.c.
	//
	// We can't use [DllImport("uno_gl_shim")] directly: the wasm runtime's pinvoke resolver
	// rejects the library name for ad-hoc statically-linked .c files. NativeLibrary.GetMainProgramHandle
	// isn't implemented on browser-wasm either. The JS path is the only thing that works.
	private static IntPtr GetShimPtr(string getterName)
	{
		var ptr = NativeMethods.GetExportedShimPtr(getterName);
		if (ptr == 0)
		{
			throw new InvalidOperationException(
				$"Native shim getter '{getterName}' is not reachable from the wasm module. " +
				"Verify that uno_gl_shim.c is being linked via WasmShellNativeCompile and that the C function is EMSCRIPTEN_KEEPALIVE-annotated.");
		}
		return (IntPtr)ptr;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTexParameteri(int target, int pname, int param) => NativeMethods.TexParameteri(target, pname, param);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glTexParameterIuiv(int target, int pname, IntPtr @params) => NativeMethods.TexParameterIuiv(target, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glReadBuffer(int mode) => NativeMethods.ReadBuffer(mode);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glReadPixels(int x, int y, int width, int height, int format, int type, IntPtr pixels)
		=> NativeMethods.ReadPixels(x, y, width, height, format, type, (int)pixels);

	// ----------------------------------------------------------------------------------------
	// Framebuffers / renderbuffers
	// ----------------------------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenFramebuffers(int n, IntPtr framebuffers) => NativeMethods.GenFramebuffers(n, (int)framebuffers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteFramebuffers(int n, IntPtr framebuffers) => NativeMethods.DeleteFramebuffers(n, (int)framebuffers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindFramebuffer(int target, uint framebuffer) => NativeMethods.BindFramebuffer(target, (int)framebuffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glCheckFramebufferStatus(int target) => NativeMethods.CheckFramebufferStatus(target);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glFramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level)
		=> NativeMethods.FramebufferTexture2D(target, attachment, textarget, (int)texture, level);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glFramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer)
		=> NativeMethods.FramebufferRenderbuffer(target, attachment, renderbuffertarget, (int)renderbuffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGenRenderbuffers(int n, IntPtr renderbuffers) => NativeMethods.GenRenderbuffers(n, (int)renderbuffers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteRenderbuffers(int n, IntPtr renderbuffers) => NativeMethods.DeleteRenderbuffers(n, (int)renderbuffers);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindRenderbuffer(int target, uint renderbuffer) => NativeMethods.BindRenderbuffer(target, (int)renderbuffer);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glRenderbufferStorage(int target, int internalformat, int width, int height)
		=> NativeMethods.RenderbufferStorage(target, internalformat, width, height);

	// ----------------------------------------------------------------------------------------
	// Shaders / programs
	// ----------------------------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static uint glCreateShader(int type) => (uint)NativeMethods.CreateShader(type);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteShader(uint shader) => NativeMethods.DeleteShader((int)shader);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glShaderSource(uint shader, int count, IntPtr strings, IntPtr lengths)
		=> NativeMethods.ShaderSource((int)shader, count, (int)strings, (int)lengths);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glCompileShader(uint shader) => NativeMethods.CompileShader((int)shader);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetShaderiv(uint shader, int pname, IntPtr @params) => NativeMethods.GetShaderiv((int)shader, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetShaderInfoLog(uint shader, int bufSize, IntPtr length, IntPtr infoLog)
		=> NativeMethods.GetShaderInfoLog((int)shader, bufSize, (int)length, (int)infoLog);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static uint glCreateProgram() => (uint)NativeMethods.CreateProgram();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDeleteProgram(uint program) => NativeMethods.DeleteProgram((int)program);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glAttachShader(uint program, uint shader) => NativeMethods.AttachShader((int)program, (int)shader);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glDetachShader(uint program, uint shader) => NativeMethods.DetachShader((int)program, (int)shader);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glLinkProgram(uint program) => NativeMethods.LinkProgram((int)program);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetProgramiv(uint program, int pname, IntPtr @params) => NativeMethods.GetProgramiv((int)program, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetProgramInfoLog(uint program, int bufSize, IntPtr length, IntPtr infoLog)
		=> NativeMethods.GetProgramInfoLog((int)program, bufSize, (int)length, (int)infoLog);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUseProgram(uint program) => NativeMethods.UseProgram((int)program);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glGetUniformLocation(uint program, IntPtr name) => NativeMethods.GetUniformLocation((int)program, (int)name);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform1i(int location, int v0) => NativeMethods.Uniform1i(location, v0);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform1f(int location, float v0) => NativeMethods.Uniform1f(location, v0);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform4f(int location, float v0, float v1, float v2, float v3)
		=> NativeMethods.Uniform4f(location, v0, v1, v2, v3);

	// ----------------------------------------------------------------------------------------
	// JS bridges. Pointer arguments and object handles are passed as int byte-offsets / ids.
	// ----------------------------------------------------------------------------------------

	private static partial class NativeMethods
	{
		private const string Prefix = "globalThis.Uno.UI.Runtime.Skia.WasmGLFunctions.";

		[JSImport(Prefix + "glGetString")]
		internal static partial int GetString(int name);

		[JSImport(Prefix + "glGetIntegerv")]
		internal static partial void GetIntegerv(int pname, int dataPtr);

		[JSImport(Prefix + "glGetError")]
		internal static partial int GetError();

		[JSImport(Prefix + "glViewport")]
		internal static partial void Viewport(int x, int y, int width, int height);

		[JSImport(Prefix + "glEnable")]
		internal static partial void Enable(int cap);

		[JSImport(Prefix + "glDisable")]
		internal static partial void Disable(int cap);

		[JSImport(Prefix + "glClear")]
		internal static partial void Clear(int mask);

		[JSImport(Prefix + "glClearColor")]
		internal static partial void ClearColor(float r, float g, float b, float a);

		[JSImport(Prefix + "glDrawArrays")]
		internal static partial void DrawArrays(int mode, int first, int count);

		[JSImport(Prefix + "glDrawElements")]
		internal static partial void DrawElements(int mode, int count, int type, int indicesPtr);

		[JSImport(Prefix + "glPixelStorei")]
		internal static partial void PixelStorei(int pname, int param);

		[JSImport(Prefix + "glGenBuffers")]
		internal static partial void GenBuffers(int n, int buffersPtr);

		[JSImport(Prefix + "glDeleteBuffers")]
		internal static partial void DeleteBuffers(int n, int buffersPtr);

		[JSImport(Prefix + "glBindBuffer")]
		internal static partial void BindBuffer(int target, int buffer);

		[JSImport(Prefix + "glBufferData")]
		internal static partial void BufferData(int target, int size, int dataPtr, int usage);

		[JSImport(Prefix + "glGenVertexArrays")]
		internal static partial void GenVertexArrays(int n, int arraysPtr);

		[JSImport(Prefix + "glDeleteVertexArrays")]
		internal static partial void DeleteVertexArrays(int n, int arraysPtr);

		[JSImport(Prefix + "glBindVertexArray")]
		internal static partial void BindVertexArray(int array);

		[JSImport(Prefix + "glVertexAttribPointer")]
		internal static partial void VertexAttribPointer(int index, int size, int type, int normalized, int stride, int pointer);

		[JSImport(Prefix + "glEnableVertexAttribArray")]
		internal static partial void EnableVertexAttribArray(int index);

		[JSImport(Prefix + "glDisableVertexAttribArray")]
		internal static partial void DisableVertexAttribArray(int index);

		[JSImport(Prefix + "glGenTextures")]
		internal static partial void GenTextures(int n, int texturesPtr);

		[JSImport(Prefix + "glDeleteTextures")]
		internal static partial void DeleteTextures(int n, int texturesPtr);

		[JSImport(Prefix + "glBindTexture")]
		internal static partial void BindTexture(int target, int texture);

		[JSImport(Prefix + "glActiveTexture")]
		internal static partial void ActiveTexture(int texture);

		[JSImport(Prefix + "glTexImage2D")]
		internal static partial void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int pixelsPtr);

		[JSImport(Prefix + "glTexParameteri")]
		internal static partial void TexParameteri(int target, int pname, int param);

		[JSImport(Prefix + "glTexParameterIuiv")]
		internal static partial void TexParameterIuiv(int target, int pname, int paramsPtr);

		[JSImport(Prefix + "glReadBuffer")]
		internal static partial void ReadBuffer(int mode);

		[JSImport(Prefix + "glReadPixels")]
		internal static partial void ReadPixels(int x, int y, int width, int height, int format, int type, int pixelsPtr);

		[JSImport(Prefix + "glGenFramebuffers")]
		internal static partial void GenFramebuffers(int n, int framebuffersPtr);

		[JSImport(Prefix + "glDeleteFramebuffers")]
		internal static partial void DeleteFramebuffers(int n, int framebuffersPtr);

		[JSImport(Prefix + "glBindFramebuffer")]
		internal static partial void BindFramebuffer(int target, int framebuffer);

		[JSImport(Prefix + "glCheckFramebufferStatus")]
		internal static partial int CheckFramebufferStatus(int target);

		[JSImport(Prefix + "glFramebufferTexture2D")]
		internal static partial void FramebufferTexture2D(int target, int attachment, int textarget, int texture, int level);

		[JSImport(Prefix + "glFramebufferRenderbuffer")]
		internal static partial void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, int renderbuffer);

		[JSImport(Prefix + "glGenRenderbuffers")]
		internal static partial void GenRenderbuffers(int n, int renderbuffersPtr);

		[JSImport(Prefix + "glDeleteRenderbuffers")]
		internal static partial void DeleteRenderbuffers(int n, int renderbuffersPtr);

		[JSImport(Prefix + "glBindRenderbuffer")]
		internal static partial void BindRenderbuffer(int target, int renderbuffer);

		[JSImport(Prefix + "glRenderbufferStorage")]
		internal static partial void RenderbufferStorage(int target, int internalformat, int width, int height);

		[JSImport(Prefix + "glCreateShader")]
		internal static partial int CreateShader(int type);

		[JSImport(Prefix + "glDeleteShader")]
		internal static partial void DeleteShader(int shader);

		[JSImport(Prefix + "glShaderSource")]
		internal static partial void ShaderSource(int shader, int count, int stringsPtr, int lengthsPtr);

		[JSImport(Prefix + "glCompileShader")]
		internal static partial void CompileShader(int shader);

		[JSImport(Prefix + "glGetShaderiv")]
		internal static partial void GetShaderiv(int shader, int pname, int paramsPtr);

		[JSImport(Prefix + "glGetShaderInfoLog")]
		internal static partial void GetShaderInfoLog(int shader, int bufSize, int lengthPtr, int infoLogPtr);

		[JSImport(Prefix + "glCreateProgram")]
		internal static partial int CreateProgram();

		[JSImport(Prefix + "glDeleteProgram")]
		internal static partial void DeleteProgram(int program);

		[JSImport(Prefix + "glAttachShader")]
		internal static partial void AttachShader(int program, int shader);

		[JSImport(Prefix + "glDetachShader")]
		internal static partial void DetachShader(int program, int shader);

		[JSImport(Prefix + "glLinkProgram")]
		internal static partial void LinkProgram(int program);

		[JSImport(Prefix + "glGetProgramiv")]
		internal static partial void GetProgramiv(int program, int pname, int paramsPtr);

		[JSImport(Prefix + "glGetProgramInfoLog")]
		internal static partial void GetProgramInfoLog(int program, int bufSize, int lengthPtr, int infoLogPtr);

		[JSImport(Prefix + "glUseProgram")]
		internal static partial void UseProgram(int program);

		[JSImport(Prefix + "glGetUniformLocation")]
		internal static partial int GetUniformLocation(int program, int namePtr);

		[JSImport(Prefix + "glUniform1i")]
		internal static partial void Uniform1i(int location, int v0);

		[JSImport(Prefix + "glUniform1f")]
		internal static partial void Uniform1f(int location, float v0);

		[JSImport(Prefix + "glUniform4f")]
		internal static partial void Uniform4f(int location, float v0, float v1, float v2, float v3);

		// Generic bootstrap helper: given a getter symbol name (uno_get_<name>_ptr), returns
		// the wasm function-table index of the wrapped C function. JS calls Module.<name> or
		// Module.cwrap(name, ...). Used for every C-shim that wraps a >8-arg gl function.
		[JSImport(Prefix + "getExportedShimPtr")]
		internal static partial int GetExportedShimPtr(string getterName);
	}
}
