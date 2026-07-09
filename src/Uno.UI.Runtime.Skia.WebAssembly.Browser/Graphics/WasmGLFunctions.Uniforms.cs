#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

// All uniform set ops (scalar + vector + matrix), getUniform queries, attribute location
// queries, validateProgram, getActiveAttrib/Uniform.
//
// Vector overloads (1iv/2iv/3iv/4iv, 1fv/2fv/3fv/4fv, 1uiv/2uiv/3uiv/4uiv) take a pointer to
// `count` * N typed values; we read them out of HEAP* on the TS side.
//
// Matrix overloads take (location, count, transpose, value_ptr). Transpose is bool but we
// pass int to keep the cookie shape simple.
internal static unsafe partial class WasmGLFunctions
{
	private static void RegisterUniformEntries()
	{
		// Scalar set: int
		_addresses["glUniform2i"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, void>)&glUniform2i;
		_addresses["glUniform3i"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, void>)&glUniform3i;
		_addresses["glUniform4i"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, int, int, void>)&glUniform4i;

		// Scalar set: float
		_addresses["glUniform2f"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, float, float, void>)&glUniform2f;
		_addresses["glUniform3f"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, float, float, float, void>)&glUniform3f;

		// Scalar set: uint (WebGL2)
		_addresses["glUniform1ui"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, void>)&glUniform1ui;
		_addresses["glUniform2ui"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, uint, void>)&glUniform2ui;
		_addresses["glUniform3ui"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, uint, uint, void>)&glUniform3ui;
		_addresses["glUniform4ui"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, uint, uint, uint, uint, void>)&glUniform4ui;

		// Vector set: int
		_addresses["glUniform1iv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform1iv;
		_addresses["glUniform2iv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform2iv;
		_addresses["glUniform3iv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform3iv;
		_addresses["glUniform4iv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform4iv;

		// Vector set: float
		_addresses["glUniform1fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform1fv;
		_addresses["glUniform2fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform2fv;
		_addresses["glUniform3fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform3fv;
		_addresses["glUniform4fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform4fv;

		// Vector set: uint (WebGL2)
		_addresses["glUniform1uiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform1uiv;
		_addresses["glUniform2uiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform2uiv;
		_addresses["glUniform3uiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform3uiv;
		_addresses["glUniform4uiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, IntPtr, void>)&glUniform4uiv;

		// Matrix set: square
		_addresses["glUniformMatrix2fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix2fv;
		_addresses["glUniformMatrix3fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix3fv;
		_addresses["glUniformMatrix4fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix4fv;

		// Matrix set: non-square (WebGL2)
		_addresses["glUniformMatrix2x3fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix2x3fv;
		_addresses["glUniformMatrix3x2fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix3x2fv;
		_addresses["glUniformMatrix2x4fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix2x4fv;
		_addresses["glUniformMatrix4x2fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix4x2fv;
		_addresses["glUniformMatrix3x4fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix3x4fv;
		_addresses["glUniformMatrix4x3fv"] = (IntPtr)(delegate* unmanaged[Cdecl]<int, int, int, IntPtr, void>)&glUniformMatrix4x3fv;

		// Get uniform value
		_addresses["glGetUniformfv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetUniformfv;
		_addresses["glGetUniformiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetUniformiv;
		_addresses["glGetUniformuiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, void>)&glGetUniformuiv;

		// Active uniform / attribute introspection
		_addresses["glGetActiveAttrib"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, IntPtr, IntPtr, IntPtr, void>)&glGetActiveAttrib;
		_addresses["glGetActiveUniform"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, IntPtr, IntPtr, IntPtr, void>)&glGetActiveUniform;
		_addresses["glGetAttribLocation"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, int>)&glGetAttribLocation;
		_addresses["glBindAttribLocation"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, IntPtr, void>)&glBindAttribLocation;
		_addresses["glValidateProgram"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, void>)&glValidateProgram;
		_addresses["glGetShaderSource"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, IntPtr, void>)&glGetShaderSource;

		// Fragment-data output location (WebGL2)
		_addresses["glGetFragDataLocation"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, int>)&glGetFragDataLocation;

		// Uniform Buffer Objects / Uniform Block introspection (WebGL2)
		_addresses["glGetUniformBlockIndex"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, IntPtr, uint>)&glGetUniformBlockIndex;
		_addresses["glUniformBlockBinding"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, uint, void>)&glUniformBlockBinding;
		_addresses["glGetActiveUniformBlockiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, void>)&glGetActiveUniformBlockiv;
		_addresses["glGetActiveUniformBlockName"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, uint, int, IntPtr, IntPtr, void>)&glGetActiveUniformBlockName;
		_addresses["glGetUniformIndices"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, IntPtr, void>)&glGetUniformIndices;
		_addresses["glGetActiveUniformsiv"] = (IntPtr)(delegate* unmanaged[Cdecl]<uint, int, IntPtr, int, IntPtr, void>)&glGetActiveUniformsiv;
	}

	// ---- UCO methods ----

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform2i(int location, int v0, int v1) => NativeMethods.Uniform2i(location, v0, v1);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform3i(int location, int v0, int v1, int v2) => NativeMethods.Uniform3i(location, v0, v1, v2);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform4i(int location, int v0, int v1, int v2, int v3) => NativeMethods.Uniform4i(location, v0, v1, v2, v3);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform2f(int location, float v0, float v1) => NativeMethods.Uniform2f(location, v0, v1);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform3f(int location, float v0, float v1, float v2) => NativeMethods.Uniform3f(location, v0, v1, v2);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform1ui(int location, uint v0) => NativeMethods.Uniform1ui(location, (int)v0);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform2ui(int location, uint v0, uint v1) => NativeMethods.Uniform2ui(location, (int)v0, (int)v1);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform3ui(int location, uint v0, uint v1, uint v2) => NativeMethods.Uniform3ui(location, (int)v0, (int)v1, (int)v2);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform4ui(int location, uint v0, uint v1, uint v2, uint v3) => NativeMethods.Uniform4ui(location, (int)v0, (int)v1, (int)v2, (int)v3);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform1iv(int location, int count, IntPtr value) => NativeMethods.Uniform1iv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform2iv(int location, int count, IntPtr value) => NativeMethods.Uniform2iv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform3iv(int location, int count, IntPtr value) => NativeMethods.Uniform3iv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform4iv(int location, int count, IntPtr value) => NativeMethods.Uniform4iv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform1fv(int location, int count, IntPtr value) => NativeMethods.Uniform1fv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform2fv(int location, int count, IntPtr value) => NativeMethods.Uniform2fv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform3fv(int location, int count, IntPtr value) => NativeMethods.Uniform3fv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform4fv(int location, int count, IntPtr value) => NativeMethods.Uniform4fv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform1uiv(int location, int count, IntPtr value) => NativeMethods.Uniform1uiv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform2uiv(int location, int count, IntPtr value) => NativeMethods.Uniform2uiv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform3uiv(int location, int count, IntPtr value) => NativeMethods.Uniform3uiv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniform4uiv(int location, int count, IntPtr value) => NativeMethods.Uniform4uiv(location, count, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix2fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix2fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix3fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix3fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix4fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix4fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix2x3fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix2x3fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix3x2fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix3x2fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix2x4fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix2x4fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix4x2fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix4x2fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix3x4fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix3x4fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformMatrix4x3fv(int location, int count, int transpose, IntPtr value) => NativeMethods.UniformMatrix4x3fv(location, count, transpose, (int)value);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetUniformfv(uint program, int location, IntPtr @params) => NativeMethods.GetUniformfv((int)program, location, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetUniformiv(uint program, int location, IntPtr @params) => NativeMethods.GetUniformiv((int)program, location, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetUniformuiv(uint program, int location, IntPtr @params) => NativeMethods.GetUniformuiv((int)program, location, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetActiveAttrib(uint program, uint index, int bufSize, IntPtr length, IntPtr size, IntPtr type, IntPtr name)
		=> NativeMethods.GetActiveAttrib((int)program, (int)index, bufSize, (int)length, (int)size, (int)type, (int)name);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetActiveUniform(uint program, uint index, int bufSize, IntPtr length, IntPtr size, IntPtr type, IntPtr name)
		=> NativeMethods.GetActiveUniform((int)program, (int)index, bufSize, (int)length, (int)size, (int)type, (int)name);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glGetAttribLocation(uint program, IntPtr name) => NativeMethods.GetAttribLocation((int)program, (int)name);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glBindAttribLocation(uint program, uint index, IntPtr name) => NativeMethods.BindAttribLocation((int)program, (int)index, (int)name);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glValidateProgram(uint program) => NativeMethods.ValidateProgram((int)program);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetShaderSource(uint shader, int bufSize, IntPtr length, IntPtr source) => NativeMethods.GetShaderSource((int)shader, bufSize, (int)length, (int)source);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int glGetFragDataLocation(uint program, IntPtr name) => NativeMethods.GetFragDataLocation((int)program, (int)name);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static uint glGetUniformBlockIndex(uint program, IntPtr uniformBlockName) => (uint)NativeMethods.GetUniformBlockIndex((int)program, (int)uniformBlockName);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glUniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding)
		=> NativeMethods.UniformBlockBinding((int)program, (int)uniformBlockIndex, (int)uniformBlockBinding);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetActiveUniformBlockiv(uint program, uint uniformBlockIndex, int pname, IntPtr @params)
		=> NativeMethods.GetActiveUniformBlockiv((int)program, (int)uniformBlockIndex, pname, (int)@params);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetActiveUniformBlockName(uint program, uint uniformBlockIndex, int bufSize, IntPtr length, IntPtr uniformBlockName)
		=> NativeMethods.GetActiveUniformBlockName((int)program, (int)uniformBlockIndex, bufSize, (int)length, (int)uniformBlockName);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetUniformIndices(uint program, int uniformCount, IntPtr uniformNames, IntPtr uniformIndices)
		=> NativeMethods.GetUniformIndices((int)program, uniformCount, (int)uniformNames, (int)uniformIndices);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void glGetActiveUniformsiv(uint program, int uniformCount, IntPtr uniformIndices, int pname, IntPtr @params)
		=> NativeMethods.GetActiveUniformsiv((int)program, uniformCount, (int)uniformIndices, pname, (int)@params);

	// ---- JS bridges ----

	private static partial class NativeMethods
	{
		[JSImport(Prefix + "glUniform2i")] internal static partial void Uniform2i(int location, int v0, int v1);
		[JSImport(Prefix + "glUniform3i")] internal static partial void Uniform3i(int location, int v0, int v1, int v2);
		[JSImport(Prefix + "glUniform4i")] internal static partial void Uniform4i(int location, int v0, int v1, int v2, int v3);
		[JSImport(Prefix + "glUniform2f")] internal static partial void Uniform2f(int location, float v0, float v1);
		[JSImport(Prefix + "glUniform3f")] internal static partial void Uniform3f(int location, float v0, float v1, float v2);
		[JSImport(Prefix + "glUniform1ui")] internal static partial void Uniform1ui(int location, int v0);
		[JSImport(Prefix + "glUniform2ui")] internal static partial void Uniform2ui(int location, int v0, int v1);
		[JSImport(Prefix + "glUniform3ui")] internal static partial void Uniform3ui(int location, int v0, int v1, int v2);
		[JSImport(Prefix + "glUniform4ui")] internal static partial void Uniform4ui(int location, int v0, int v1, int v2, int v3);

		[JSImport(Prefix + "glUniform1iv")] internal static partial void Uniform1iv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform2iv")] internal static partial void Uniform2iv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform3iv")] internal static partial void Uniform3iv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform4iv")] internal static partial void Uniform4iv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform1fv")] internal static partial void Uniform1fv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform2fv")] internal static partial void Uniform2fv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform3fv")] internal static partial void Uniform3fv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform4fv")] internal static partial void Uniform4fv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform1uiv")] internal static partial void Uniform1uiv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform2uiv")] internal static partial void Uniform2uiv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform3uiv")] internal static partial void Uniform3uiv(int location, int count, int valuePtr);
		[JSImport(Prefix + "glUniform4uiv")] internal static partial void Uniform4uiv(int location, int count, int valuePtr);

		[JSImport(Prefix + "glUniformMatrix2fv")] internal static partial void UniformMatrix2fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix3fv")] internal static partial void UniformMatrix3fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix4fv")] internal static partial void UniformMatrix4fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix2x3fv")] internal static partial void UniformMatrix2x3fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix3x2fv")] internal static partial void UniformMatrix3x2fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix2x4fv")] internal static partial void UniformMatrix2x4fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix4x2fv")] internal static partial void UniformMatrix4x2fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix3x4fv")] internal static partial void UniformMatrix3x4fv(int location, int count, int transpose, int valuePtr);
		[JSImport(Prefix + "glUniformMatrix4x3fv")] internal static partial void UniformMatrix4x3fv(int location, int count, int transpose, int valuePtr);

		[JSImport(Prefix + "glGetUniformfv")] internal static partial void GetUniformfv(int program, int location, int paramsPtr);
		[JSImport(Prefix + "glGetUniformiv")] internal static partial void GetUniformiv(int program, int location, int paramsPtr);
		[JSImport(Prefix + "glGetUniformuiv")] internal static partial void GetUniformuiv(int program, int location, int paramsPtr);

		[JSImport(Prefix + "glGetActiveAttrib")] internal static partial void GetActiveAttrib(int program, int index, int bufSize, int lengthPtr, int sizePtr, int typePtr, int namePtr);
		[JSImport(Prefix + "glGetActiveUniform")] internal static partial void GetActiveUniform(int program, int index, int bufSize, int lengthPtr, int sizePtr, int typePtr, int namePtr);
		[JSImport(Prefix + "glGetAttribLocation")] internal static partial int GetAttribLocation(int program, int namePtr);
		[JSImport(Prefix + "glBindAttribLocation")] internal static partial void BindAttribLocation(int program, int index, int namePtr);
		[JSImport(Prefix + "glValidateProgram")] internal static partial void ValidateProgram(int program);
		[JSImport(Prefix + "glGetShaderSource")] internal static partial void GetShaderSource(int shader, int bufSize, int lengthPtr, int sourcePtr);
		[JSImport(Prefix + "glGetFragDataLocation")] internal static partial int GetFragDataLocation(int program, int namePtr);

		[JSImport(Prefix + "glGetUniformBlockIndex")] internal static partial int GetUniformBlockIndex(int program, int namePtr);
		[JSImport(Prefix + "glUniformBlockBinding")] internal static partial void UniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding);
		[JSImport(Prefix + "glGetActiveUniformBlockiv")] internal static partial void GetActiveUniformBlockiv(int program, int uniformBlockIndex, int pname, int paramsPtr);
		[JSImport(Prefix + "glGetActiveUniformBlockName")] internal static partial void GetActiveUniformBlockName(int program, int uniformBlockIndex, int bufSize, int lengthPtr, int namePtr);
		[JSImport(Prefix + "glGetUniformIndices")] internal static partial void GetUniformIndices(int program, int uniformCount, int uniformNamesPtr, int uniformIndicesPtr);
		[JSImport(Prefix + "glGetActiveUniformsiv")] internal static partial void GetActiveUniformsiv(int program, int uniformCount, int uniformIndicesPtr, int pname, int paramsPtr);
	}
}
