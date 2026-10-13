#nullable enable

using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Wayland.Tests;

// GLES 2 with RGBA8 support keeps Skia's capability probes deterministic and needs no display or GPU.
internal sealed unsafe class MockGles : IGLDeviceContext
{
	[ThreadStatic]
	private static MockGles? _current;
	private readonly nint _version = Marshal.StringToHGlobalAnsi("OpenGL ES 2.0");
	private readonly nint _shaderVersion = Marshal.StringToHGlobalAnsi("OpenGL ES GLSL ES 1.00");
	private readonly nint _vendor = Marshal.StringToHGlobalAnsi("Uno test");
	private readonly nint _extensions = Marshal.StringToHGlobalAnsi("GL_OES_rgb8_rgba8");
	private uint _nextName;

	public MockGles() => _current = this;

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;
	public Func<string, nint> GetProcAddress => Load;
	public bool ContextLost { get; set; }
	public int Calls { get; private set; }
	public int CallsAfterContextLoss { get; private set; }
	public int GeneratedTextures { get; private set; }
	public int DeletedTextures { get; private set; }

	private static void RecordCall()
	{
		var gl = _current!;
		gl.Calls++;
		if (gl.ContextLost)
		{
			gl.CallsAfterContextLoss++;
		}
	}

	private static nint Load(string name) => name switch
	{
		"glGetString" => (nint)(delegate* unmanaged<uint, nint>)&GetString,
		"glGetIntegerv" => (nint)(delegate* unmanaged<uint, nint, void>)&GetInteger,
		"glGetShaderPrecisionFormat" => (nint)(delegate* unmanaged<uint, uint, nint, nint, void>)&GetShaderPrecision,
		"glGenTextures" => (nint)(delegate* unmanaged<int, nint, void>)&GenerateTextures,
		"glGenBuffers" => (nint)(delegate* unmanaged<int, nint, void>)&GenerateNames,
		"glGenFramebuffers" => (nint)(delegate* unmanaged<int, nint, void>)&GenerateNames,
		"glGenRenderbuffers" => (nint)(delegate* unmanaged<int, nint, void>)&GenerateNames,
		"glCheckFramebufferStatus" => (nint)(delegate* unmanaged<uint, uint>)&CheckFramebuffer,
		"glDeleteTextures" => (nint)(delegate* unmanaged<int, nint, void>)&DeleteTextures,
		"glDeleteBuffers" => (nint)(delegate* unmanaged<int, nint, void>)&DeleteNames,
		"glDeleteFramebuffers" => (nint)(delegate* unmanaged<int, nint, void>)&DeleteNames,
		"glDeleteRenderbuffers" => (nint)(delegate* unmanaged<int, nint, void>)&DeleteNames,
		"glActiveTexture" or "glBlendEquation" or "glClear" or
		"glCompileShader" or "glCullFace" or "glDeleteProgram" or
		"glDeleteShader" or "glDisable" or "glDisableVertexAttribArray" or
		"glEnable" or "glEnableVertexAttribArray" or "glFrontFace" or
		"glGenerateMipmap" or "glLinkProgram" or "glStencilMask" or
		"glUseProgram" => (nint)(delegate* unmanaged<uint, void>)&ActiveTexture,
		"glAttachShader" or "glBindBuffer" or "glBindFramebuffer" or
		"glBindRenderbuffer" or "glBindTexture" or "glBlendFunc" or
		"glStencilMaskSeparate" => (nint)(delegate* unmanaged<uint, uint, void>)&AttachShader,
		"glBindAttribLocation" or "glGetBufferParameteriv" or "glGetProgramiv" or
		"glGetRenderbufferParameteriv" or "glGetShaderiv" or "glTexParameterfv" or
		"glTexParameteriv" => (nint)(delegate* unmanaged<uint, uint, nint, void>)&BindAttribLocation,
		"glBlendColor" or "glClearColor" => (nint)(delegate* unmanaged<float, float, float, float, void>)&BlendColor,
		"glBufferData" => (nint)(delegate* unmanaged<uint, nint, nint, uint, void>)&BufferData,
		"glBufferSubData" => (nint)(delegate* unmanaged<uint, nint, nint, nint, void>)&BufferSubData,
		"glClearStencil" => (nint)(delegate* unmanaged<int, void>)&ClearStencil,
		"glColorMask" => (nint)(delegate* unmanaged<byte, byte, byte, byte, void>)&ColorMask,
		"glCompressedTexImage2D" => (nint)(delegate* unmanaged<uint, int, uint, int, int, int, int, nint, void>)&CompressedTexImage2D,
		"glCompressedTexSubImage2D" => (nint)(delegate* unmanaged<uint, int, int, int, int, int, uint, int, nint, void>)&CompressedTexSubImage2D,
		"glCopyTexSubImage2D" => (nint)(delegate* unmanaged<uint, int, int, int, int, int, int, int, void>)&CopyTexSubImage2D,
		"glCreateProgram" or "glGetError" => (nint)(delegate* unmanaged<uint>)&CreateProgram,
		"glCreateShader" => (nint)(delegate* unmanaged<uint, uint>)&CreateShader,
		"glDepthMask" => (nint)(delegate* unmanaged<byte, void>)&DepthMask,
		"glDrawArrays" => (nint)(delegate* unmanaged<uint, int, int, void>)&DrawArrays,
		"glDrawElements" => (nint)(delegate* unmanaged<uint, int, uint, nint, void>)&DrawElements,
		"glFinish" or "glFlush" => (nint)(delegate* unmanaged<void>)&Finish,
		"glFramebufferRenderbuffer" or "glStencilOpSeparate" => (nint)(delegate* unmanaged<uint, uint, uint, uint, void>)&FramebufferRenderbuffer,
		"glFramebufferTexture2D" => (nint)(delegate* unmanaged<uint, uint, uint, uint, int, void>)&FramebufferTexture2D,
		"glGetFloatv" or "glVertexAttrib2fv" or "glVertexAttrib3fv" or
		"glVertexAttrib4fv" => (nint)(delegate* unmanaged<uint, nint, void>)&GetFloatv,
		"glGetFramebufferAttachmentParameteriv" => (nint)(delegate* unmanaged<uint, uint, uint, nint, void>)&GetFramebufferAttachmentParameteriv,
		"glGetProgramInfoLog" or "glGetShaderInfoLog" or "glShaderSource" => (nint)(delegate* unmanaged<uint, int, nint, nint, void>)&GetProgramInfoLog,
		"glGetUniformLocation" => (nint)(delegate* unmanaged<uint, nint, int>)&GetUniformLocation,
		"glIsTexture" => (nint)(delegate* unmanaged<uint, byte>)&IsTexture,
		"glLineWidth" => (nint)(delegate* unmanaged<float, void>)&LineWidth,
		"glPixelStorei" => (nint)(delegate* unmanaged<uint, int, void>)&PixelStorei,
		"glReadPixels" => (nint)(delegate* unmanaged<int, int, int, int, uint, uint, nint, void>)&ReadPixels,
		"glRenderbufferStorage" => (nint)(delegate* unmanaged<uint, uint, int, int, void>)&RenderbufferStorage,
		"glScissor" or "glUniform3i" or "glViewport" => (nint)(delegate* unmanaged<int, int, int, int, void>)&Scissor,
		"glStencilFunc" => (nint)(delegate* unmanaged<uint, int, uint, void>)&StencilFunc,
		"glStencilFuncSeparate" => (nint)(delegate* unmanaged<uint, uint, int, uint, void>)&StencilFuncSeparate,
		"glStencilOp" => (nint)(delegate* unmanaged<uint, uint, uint, void>)&StencilOp,
		"glTexImage2D" or "glTexSubImage2D" => (nint)(delegate* unmanaged<uint, int, int, int, int, int, uint, uint, nint, void>)&TexImage2D,
		"glTexParameterf" => (nint)(delegate* unmanaged<uint, uint, float, void>)&TexParameterf,
		"glTexParameteri" => (nint)(delegate* unmanaged<uint, uint, int, void>)&TexParameteri,
		"glUniform1f" => (nint)(delegate* unmanaged<int, float, void>)&Uniform1f,
		"glUniform1fv" or "glUniform1iv" or "glUniform2fv" or
		"glUniform2iv" or "glUniform3fv" or "glUniform3iv" or
		"glUniform4fv" or "glUniform4iv" => (nint)(delegate* unmanaged<int, int, nint, void>)&Uniform1fv,
		"glUniform1i" => (nint)(delegate* unmanaged<int, int, void>)&Uniform1i,
		"glUniform2f" => (nint)(delegate* unmanaged<int, float, float, void>)&Uniform2f,
		"glUniform2i" => (nint)(delegate* unmanaged<int, int, int, void>)&Uniform2i,
		"glUniform3f" => (nint)(delegate* unmanaged<int, float, float, float, void>)&Uniform3f,
		"glUniform4f" => (nint)(delegate* unmanaged<int, float, float, float, float, void>)&Uniform4f,
		"glUniform4i" => (nint)(delegate* unmanaged<int, int, int, int, int, void>)&Uniform4i,
		"glUniformMatrix2fv" or "glUniformMatrix3fv" or "glUniformMatrix4fv" => (nint)(delegate* unmanaged<int, int, byte, nint, void>)&UniformMatrix2fv,
		"glVertexAttrib1f" => (nint)(delegate* unmanaged<uint, float, void>)&VertexAttrib1f,
		"glVertexAttribPointer" => (nint)(delegate* unmanaged<uint, int, uint, byte, int, nint, void>)&VertexAttribPointer,
		_ => 0,
	};

	[UnmanagedCallersOnly]
	private static nint GetString(uint name)
	{
		RecordCall();
		var gl = _current!;
		return name switch
		{
			0x1F02 => gl._version,
			0x8B8C => gl._shaderVersion,
			0x1F03 => gl._extensions,
			_ => gl._vendor,
		};
	}

	[UnmanagedCallersOnly]
	private static void GetInteger(uint name, nint value)
	{
		RecordCall();
		*(int*)value = name switch
		{
			0x0D33 or 0x84E8 => 4096, // Maximum texture/renderbuffer size.
			0x8869 or 0x8DFD => 16, // Vertex attributes / fragment uniform vectors.
			0x8872 => 8, // Texture image units.
			_ => 0,
		};
	}

	[UnmanagedCallersOnly]
	private static void GetShaderPrecision(uint stage, uint kind, nint range, nint precision)
	{
		RecordCall();
		((int*)range)[0] = 127;
		((int*)range)[1] = 127;
		*(int*)precision = 23;
	}

	private static void Generate(int count, nint names)
	{
		RecordCall();
		for (var i = 0; i < count; i++)
		{
			((uint*)names)[i] = ++_current!._nextName;
		}
	}

	[UnmanagedCallersOnly]
	private static void GenerateTextures(int count, nint names)
	{
		Generate(count, names);
		_current!.GeneratedTextures += count;
	}

	[UnmanagedCallersOnly]
	private static void GenerateNames(int count, nint names) => Generate(count, names);

	[UnmanagedCallersOnly]
	private static void DeleteTextures(int count, nint names)
	{
		RecordCall();
		_current!.DeletedTextures += count;
	}

	[UnmanagedCallersOnly]
	private static void DeleteNames(int count, nint names) => RecordCall();

	[UnmanagedCallersOnly]
	private static uint CheckFramebuffer(uint target)
	{
		RecordCall();
		return 0x8CD5;
	}

	[UnmanagedCallersOnly]
	private static void ActiveTexture(uint arg0) => RecordCall();

	[UnmanagedCallersOnly]
	private static void AttachShader(uint arg0, uint arg1) => RecordCall();

	[UnmanagedCallersOnly]
	private static void BindAttribLocation(uint arg0, uint arg1, nint arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void BlendColor(float arg0, float arg1, float arg2, float arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void BufferData(uint arg0, nint arg1, nint arg2, uint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void BufferSubData(uint arg0, nint arg1, nint arg2, nint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void ClearStencil(int arg0) => RecordCall();

	[UnmanagedCallersOnly]
	private static void ColorMask(byte arg0, byte arg1, byte arg2, byte arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void CompressedTexImage2D(uint arg0, int arg1, uint arg2, int arg3, int arg4, int arg5, int arg6, nint arg7) => RecordCall();

	[UnmanagedCallersOnly]
	private static void CompressedTexSubImage2D(uint arg0, int arg1, int arg2, int arg3, int arg4, int arg5, uint arg6, int arg7, nint arg8) => RecordCall();

	[UnmanagedCallersOnly]
	private static void CopyTexSubImage2D(uint arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7) => RecordCall();

	[UnmanagedCallersOnly]
	private static uint CreateProgram()
	{
		RecordCall();
		return 0;
	}

	[UnmanagedCallersOnly]
	private static uint CreateShader(uint arg0)
	{
		RecordCall();
		return 0;
	}

	[UnmanagedCallersOnly]
	private static void DepthMask(byte arg0) => RecordCall();

	[UnmanagedCallersOnly]
	private static void DrawArrays(uint arg0, int arg1, int arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void DrawElements(uint arg0, int arg1, uint arg2, nint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Finish() => RecordCall();

	[UnmanagedCallersOnly]
	private static void FramebufferRenderbuffer(uint arg0, uint arg1, uint arg2, uint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void FramebufferTexture2D(uint arg0, uint arg1, uint arg2, uint arg3, int arg4) => RecordCall();

	[UnmanagedCallersOnly]
	private static void GetFloatv(uint arg0, nint arg1) => RecordCall();

	[UnmanagedCallersOnly]
	private static void GetFramebufferAttachmentParameteriv(uint arg0, uint arg1, uint arg2, nint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void GetProgramInfoLog(uint arg0, int arg1, nint arg2, nint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static int GetUniformLocation(uint arg0, nint arg1)
	{
		RecordCall();
		return 0;
	}

	[UnmanagedCallersOnly]
	private static byte IsTexture(uint arg0)
	{
		RecordCall();
		return 0;
	}

	[UnmanagedCallersOnly]
	private static void LineWidth(float arg0) => RecordCall();

	[UnmanagedCallersOnly]
	private static void PixelStorei(uint arg0, int arg1) => RecordCall();

	[UnmanagedCallersOnly]
	private static void ReadPixels(int arg0, int arg1, int arg2, int arg3, uint arg4, uint arg5, nint arg6) => RecordCall();

	[UnmanagedCallersOnly]
	private static void RenderbufferStorage(uint arg0, uint arg1, int arg2, int arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Scissor(int arg0, int arg1, int arg2, int arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void StencilFunc(uint arg0, int arg1, uint arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void StencilFuncSeparate(uint arg0, uint arg1, int arg2, uint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void StencilOp(uint arg0, uint arg1, uint arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void TexImage2D(uint arg0, int arg1, int arg2, int arg3, int arg4, int arg5, uint arg6, uint arg7, nint arg8) => RecordCall();

	[UnmanagedCallersOnly]
	private static void TexParameterf(uint arg0, uint arg1, float arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void TexParameteri(uint arg0, uint arg1, int arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform1f(int arg0, float arg1) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform1fv(int arg0, int arg1, nint arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform1i(int arg0, int arg1) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform2f(int arg0, float arg1, float arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform2i(int arg0, int arg1, int arg2) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform3f(int arg0, float arg1, float arg2, float arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform4f(int arg0, float arg1, float arg2, float arg3, float arg4) => RecordCall();

	[UnmanagedCallersOnly]
	private static void Uniform4i(int arg0, int arg1, int arg2, int arg3, int arg4) => RecordCall();

	[UnmanagedCallersOnly]
	private static void UniformMatrix2fv(int arg0, int arg1, byte arg2, nint arg3) => RecordCall();

	[UnmanagedCallersOnly]
	private static void VertexAttrib1f(uint arg0, float arg1) => RecordCall();

	[UnmanagedCallersOnly]
	private static void VertexAttribPointer(uint arg0, int arg1, uint arg2, byte arg3, int arg4, nint arg5) => RecordCall();

	public void Dispose()
	{
		Marshal.FreeHGlobal(_version);
		Marshal.FreeHGlobal(_shaderVersion);
		Marshal.FreeHGlobal(_vendor);
		Marshal.FreeHGlobal(_extensions);
		_current = null;
	}
}
