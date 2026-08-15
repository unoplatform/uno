#nullable enable

using System;
using Android.Opengl;
using Android.Runtime;
using Java.Nio;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Software (CPU-raster) <see cref="ISwapChain"/> for the Android Skia path, living inside the same
/// <c>GLSurfaceView</c> as <see cref="AndroidGLGraphicsContext"/> — chosen when <c>UseOpenGLOnSkiaAndroid</c> is
/// false. It mirrors feature/breakingchanges' "render CPU, present via GL": the Skia backend rasterizes the frame
/// into a CPU framebuffer (a neutral <see cref="ISoftwareRenderTarget"/>, RGBA8888 — the backend honors
/// <see cref="IRenderTarget.ColorFormat"/>, so no channel swizzle), then <see cref="Present"/> uploads that buffer
/// as a texture and blits it over the default framebuffer with a trivial GLES2 quad; <c>GLSurfaceView</c> swaps
/// implicitly once <c>OnDrawFrame</c> returns. Names no Skia type. The GL context is current on the GLSurfaceView
/// render thread when acquired/presented, so all GL calls are valid here.
/// </summary>
internal sealed class AndroidSoftwareGraphicsContext : ISwapChain
{
	// Interleaved fullscreen triangle-strip: pos.xy (NDC) + tex.uv. V is flipped (CPU buffer is top-down; the top of
	// the screen, pos.y=+1, samples the first buffer row, tex.v=0).
	private static readonly float[] _quad =
	{
		-1f, -1f, 0f, 1f,
		 1f, -1f, 1f, 1f,
		-1f,  1f, 0f, 0f,
		 1f,  1f, 1f, 0f,
	};

	private const string VertexShaderSource =
		"attribute vec2 aPos;\n" +
		"attribute vec2 aTex;\n" +
		"varying vec2 vTex;\n" +
		"void main() { vTex = aTex; gl_Position = vec4(aPos, 0.0, 1.0); }\n";

	private const string FragmentShaderSource =
		"precision mediump float;\n" +
		"varying vec2 vTex;\n" +
		"uniform sampler2D uTex;\n" +
		"void main() { gl_FragColor = texture2D(uTex, vTex); }\n";

	private ByteBuffer? _buffer;
	private nint _pixels;
	private int _width, _height;

	private bool _glInitialized;
	private int _program;
	private int _texture;
	private int _posLocation;
	private int _texLocation;
	private FloatBuffer? _quadBuffer;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_buffer is null || width != _width || height != _height)
		{
			_buffer?.Dispose();
			_buffer = ByteBuffer.AllocateDirect(width * height * 4);
			_pixels = JNIEnv.GetDirectBufferAddress(_buffer.Handle);
			_width = width;
			_height = height;
		}

		return new AndroidSoftwareRenderTarget(_pixels, width * 4, width, height);
	}

	public void Present()
	{
		if (_buffer is null)
		{
			return;
		}

		EnsureGL();

		GLES20.GlViewport(0, 0, _width, _height);
		GLES20.GlDisable(GLES20.GlBlend);
		GLES20.GlUseProgram(_program);

		GLES20.GlActiveTexture(GLES20.GlTexture0);
		GLES20.GlBindTexture(GLES20.GlTexture2d, _texture);
		_buffer.Position(0);
		GLES20.GlTexImage2D(GLES20.GlTexture2d, 0, GLES20.GlRgba, _width, _height, 0, GLES20.GlRgba, GLES20.GlUnsignedByte, _buffer);

		_quadBuffer!.Position(0);
		GLES20.GlEnableVertexAttribArray(_posLocation);
		GLES20.GlVertexAttribPointer(_posLocation, 2, GLES20.GlFloat, false, 4 * sizeof(float), _quadBuffer);
		_quadBuffer.Position(2);
		GLES20.GlEnableVertexAttribArray(_texLocation);
		GLES20.GlVertexAttribPointer(_texLocation, 2, GLES20.GlFloat, false, 4 * sizeof(float), _quadBuffer);

		GLES20.GlDrawArrays(GLES20.GlTriangleStrip, 0, 4);

		GLES20.GlDisableVertexAttribArray(_posLocation);
		GLES20.GlDisableVertexAttribArray(_texLocation);
	}

	private void EnsureGL()
	{
		if (_glInitialized)
		{
			return;
		}

		_program = LinkProgram(VertexShaderSource, FragmentShaderSource);
		_posLocation = GLES20.GlGetAttribLocation(_program, "aPos");
		_texLocation = GLES20.GlGetAttribLocation(_program, "aTex");

		var textures = new int[1];
		GLES20.GlGenTextures(1, textures, 0);
		_texture = textures[0];
		GLES20.GlBindTexture(GLES20.GlTexture2d, _texture);
		GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, GLES20.GlLinear);
		GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, GLES20.GlLinear);
		GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
		GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);

		_quadBuffer = ByteBuffer.AllocateDirect(_quad.Length * sizeof(float))
			.Order(ByteOrder.NativeOrder()!)!
			.AsFloatBuffer()!;
		_quadBuffer.Put(_quad);
		_quadBuffer.Position(0);

		_glInitialized = true;
	}

	private static int LinkProgram(string vertexSource, string fragmentSource)
	{
		var vertex = CompileShader(GLES20.GlVertexShader, vertexSource);
		var fragment = CompileShader(GLES20.GlFragmentShader, fragmentSource);
		var program = GLES20.GlCreateProgram();
		GLES20.GlAttachShader(program, vertex);
		GLES20.GlAttachShader(program, fragment);
		GLES20.GlLinkProgram(program);

		var status = new int[1];
		GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, status, 0);
		if (status[0] == 0)
		{
			var log = GLES20.GlGetProgramInfoLog(program);
			typeof(AndroidSoftwareGraphicsContext).Log().Error($"Software present program link failed: {log}");
		}

		// The shaders are owned by the program once attached; flag them for deletion.
		GLES20.GlDeleteShader(vertex);
		GLES20.GlDeleteShader(fragment);
		return program;
	}

	private static int CompileShader(int type, string source)
	{
		var shader = GLES20.GlCreateShader(type);
		GLES20.GlShaderSource(shader, source);
		GLES20.GlCompileShader(shader);

		var status = new int[1];
		GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, status, 0);
		if (status[0] == 0)
		{
			var log = GLES20.GlGetShaderInfoLog(shader);
			typeof(AndroidSoftwareGraphicsContext).Log().Error($"Software present shader compile failed: {log}");
		}

		return shader;
	}

	public void Dispose()
	{
		if (_glInitialized)
		{
			if (_texture != 0)
			{
				GLES20.GlDeleteTextures(1, new[] { _texture }, 0);
				_texture = 0;
			}
			if (_program != 0)
			{
				GLES20.GlDeleteProgram(_program);
				_program = 0;
			}
			_quadBuffer?.Dispose();
			_quadBuffer = null;
			_glInitialized = false;
		}

		_buffer?.Dispose();
		_buffer = null;
		_pixels = 0;
	}

	// Neutral CPU framebuffer target; the Skia backend wraps it as an RGBA8888 surface (ColorFormat-driven).
	private sealed class AndroidSoftwareRenderTarget(nint pixels, int rowBytes, int width, int height) : ISoftwareRenderTarget
	{
		public nint Pixels => pixels;
		public int RowBytes => rowBytes;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}
}
