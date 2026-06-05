#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests pixel buffer objects (PBOs), where the pixels argument of the upload and
	// readback calls becomes a byte OFFSET into the bound buffer instead of a memory pointer:
	//   - PIXEL_UNPACK_BUFFER: gl.TexImage2D sourcing from a PBO (offset 0) and per-frame
	//     gl.TexSubImage2D sourcing single rows from non-zero offsets within the PBO
	//   - PIXEL_PACK_BUFFER: gl.ReadPixels writing into a PBO
	//   - gl.GetBufferSubData reading the packed pixels back to CPU memory
	//
	// Init performs a full byte-exact round-trip self-check: checkerboard -> unpack PBO ->
	// texture -> FBO -> ReadPixels -> pack PBO -> GetBufferSubData -> compare. A visible
	// (scrolling-band checkerboard) quad means the round-trip passed; Init throws otherwise.
	public class GLCanvasElement_PixelBuffersElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int TexSize = 64;

		private uint _texture, _unpackPbo, _packPbo, _fbo;
		private uint _vao, _vbo, _program;
		private int _uTexLoc;
		private int _scrollRow;
		private DateTime _startTime;

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			// --- Source data: RGBA checkerboard ---
			var pixels = new byte[TexSize * TexSize * 4];
			for (int y = 0; y < TexSize; y++)
			{
				for (int x = 0; x < TexSize; x++)
				{
					var check = ((x / 8) + (y / 8)) % 2 == 0;
					var i = (y * TexSize + x) * 4;
					pixels[i + 0] = check ? (byte)90 : (byte)230;
					pixels[i + 1] = check ? (byte)160 : (byte)120;
					pixels[i + 2] = check ? (byte)230 : (byte)60;
					pixels[i + 3] = 255;
				}
			}

			// --- Upload path: CPU -> unpack PBO -> texture (pixels arg = offset 0) ---
			_unpackPbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, _unpackPbo);
			gl.BufferData(BufferTargetARB.PixelUnpackBuffer, new ReadOnlySpan<byte>(pixels), BufferUsageARB.StreamDraw);

			_texture = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, _texture);
			gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, TexSize, TexSize, 0, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)0);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Nearest);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Nearest);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (uint)GLEnum.ClampToEdge);
			gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);

			// --- Readback path: texture -> FBO -> ReadPixels -> pack PBO ---
			_fbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, _fbo);
			gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, _texture, 0);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("Readback FBO is not complete");
			}

			_packPbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.PixelPackBuffer, _packPbo);
			gl.BufferData(BufferTargetARB.PixelPackBuffer, (nuint)pixels.Length, null, BufferUsageARB.StreamRead);
			gl.ReadPixels(0, 0, TexSize, TexSize, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)0);
			gl.BindFramebuffer(GLEnum.Framebuffer, 0);

			// --- Round-trip self-check: pack PBO -> CPU, byte-exact against the source ---
			var readBack = new byte[pixels.Length];
			fixed (byte* p = readBack)
			{
				gl.GetBufferSubData(BufferTargetARB.PixelPackBuffer, 0, (nuint)readBack.Length, p);
			}
			gl.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);
			for (int i = 0; i < pixels.Length; i++)
			{
				if (readBack[i] != pixels[i])
				{
					throw new Exception($"PBO round-trip mismatch at byte {i}: got {readBack[i]}, expected {pixels[i]}");
				}
			}

			// --- Geometry + shader ---
			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			var quad = new float[]
			{
				-1f, -1f,     0f, 0f,
				 1f, -1f,     1f, 0f,
				 1f,  1f,     1f, 1f,
				-1f, -1f,     0f, 0f,
				 1f,  1f,     1f, 1f,
				-1f,  1f,     0f, 1f,
			};
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(quad), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			_program = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				layout(location = 1) in vec2 aUV;
				out vec2 vUV;
				void main() {
				    gl_Position = vec4(aPos, 0.0, 1.0);
				    vUV = aUV;
				}
				""",
				versionDef + """

				precision highp float;
				in vec2 vUV;
				out vec4 fragColor;
				uniform sampler2D uTex;
				void main() { fragColor = texture(uTex, vUV); }
				""");
			_uTexLoc = gl.GetUniformLocation(_program, "uTex");
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteTexture(_texture);
			gl.DeleteBuffer(_unpackPbo);
			gl.DeleteBuffer(_packPbo);
			gl.DeleteFramebuffer(_fbo);
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteProgram(_program);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			// --- Scroll a rainbow band through the texture, one row per frame, sourced from a
			// NON-ZERO offset within the unpack PBO (offset = the row's own position). ---
			var row = new byte[TexSize * 4];
			for (int x = 0; x < TexSize; x++)
			{
				var hue = (x / (float)TexSize + t * 0.3f) % 1f;
				var (r, g, b) = HueToRgb(hue);
				row[x * 4 + 0] = r;
				row[x * 4 + 1] = g;
				row[x * 4 + 2] = b;
				row[x * 4 + 3] = 255;
			}
			var rowByteOffset = _scrollRow * TexSize * 4;
			gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, _unpackPbo);
			gl.BufferSubData(BufferTargetARB.PixelUnpackBuffer, rowByteOffset, new ReadOnlySpan<byte>(row));
			gl.BindTexture(TextureTarget.Texture2D, _texture);
			gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, _scrollRow, TexSize, 1, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)rowByteOffset);
			gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
			_scrollRow = (_scrollRow + 1) % TexSize;

			gl.ClearColor(0.05f, 0.05f, 0.07f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);

			gl.UseProgram(_program);
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, _texture);
			gl.Uniform1(_uTexLoc, 0);
			gl.BindVertexArray(_vao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

			Invalidate();
		}

		private static (byte, byte, byte) HueToRgb(float hue)
		{
			var h = hue * 6f;
			var x = (byte)(255 * (1 - Math.Abs(h % 2 - 1)));
			return ((int)h % 6) switch
			{
				0 => ((byte)255, x, (byte)0),
				1 => (x, (byte)255, (byte)0),
				2 => ((byte)0, (byte)255, x),
				3 => ((byte)0, x, (byte)255),
				4 => (x, (byte)0, (byte)255),
				_ => ((byte)255, (byte)0, x),
			};
		}

		private static uint CreateProgram(GL gl, string vertexSource, string fragmentSource)
		{
			var vs = CompileShader(gl, ShaderType.VertexShader, vertexSource);
			var fs = CompileShader(gl, ShaderType.FragmentShader, fragmentSource);
			var prog = gl.CreateProgram();
			gl.AttachShader(prog, vs);
			gl.AttachShader(prog, fs);
			gl.LinkProgram(prog);
			gl.GetProgram(prog, ProgramPropertyARB.LinkStatus, out int linkStatus);
			if (linkStatus != (int)GLEnum.True)
			{
				throw new Exception("Program link failed: " + gl.GetProgramInfoLog(prog));
			}
			gl.DetachShader(prog, vs);
			gl.DetachShader(prog, fs);
			gl.DeleteShader(vs);
			gl.DeleteShader(fs);
			return prog;
		}

		private static uint CompileShader(GL gl, ShaderType type, string source)
		{
			var sh = gl.CreateShader(type);
			gl.ShaderSource(sh, source);
			gl.CompileShader(sh);
			gl.GetShader(sh, ShaderParameterName.CompileStatus, out int status);
			if (status != (int)GLEnum.True)
			{
				throw new Exception($"{type} compile failed: " + gl.GetShaderInfoLog(sh));
			}
			return sh;
		}
	}
}
#endif
