#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests the texturing pipeline:
	//   - gl.TexImage2D with real pixel data (goes through the WASM C-shim for >8-arg dispatch)
	//   - gl.TexParameteri (min/mag/wrap)
	//   - gl.ActiveTexture / sampler2D binding
	//   - gl.Uniform1i (sampler unit) + gl.Uniform2f (animated UV offset)
	public class GLCanvasElement_TexturedQuadElement() : GLCanvasElement(() => App.MainWindow)
	{
		private uint _vao;
		private uint _vbo;
		private uint _program;
		private uint _texture;
		private int _uOffsetLoc;
		private int _uTexLoc;
		private DateTime _startTime;

		// 6 vertices, two triangles forming a fullscreen-ish quad.
		// Layout: vec2 position, vec2 uv  -> stride = 4 floats.
		private static readonly float[] _vertices =
		{
			// pos        // uv
			-0.8f, -0.8f, 0f, 0f,
			 0.8f, -0.8f, 1f, 0f,
			 0.8f,  0.8f, 1f, 1f,

			-0.8f, -0.8f, 0f, 0f,
			 0.8f,  0.8f, 1f, 1f,
			-0.8f,  0.8f, 0f, 1f,
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			// VAO + VBO
			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);

			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_vertices), BufferUsageARB.StaticDraw);

			// position attribute (location 0)
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			// uv attribute (location 1)
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);

			// Procedural 64x64 RGBA checkerboard
			const int size = 64;
			var pixels = new byte[size * size * 4];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					var cellX = x / 8;
					var cellY = y / 8;
					var even = ((cellX + cellY) & 1) == 0;
					var idx = (y * size + x) * 4;
					pixels[idx + 0] = even ? (byte)230 : (byte)32;  // R
					pixels[idx + 1] = even ? (byte)90 : (byte)220;  // G
					pixels[idx + 2] = even ? (byte)40 : (byte)80;   // B
					pixels[idx + 3] = 255;                          // A
				}
			}

			_texture = gl.GenTexture();
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, _texture);
			fixed (byte* pPixels = pixels)
			{
				gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, size, size, 0, GLEnum.Rgba, GLEnum.UnsignedByte, pPixels);
			}
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (uint)GLEnum.Repeat);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (uint)GLEnum.Repeat);

			// Shaders
			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			var vertexCode = versionDef + """

			precision highp float;
			layout(location = 0) in vec2 aPos;
			layout(location = 1) in vec2 aUV;
			out vec2 vUV;
			uniform vec2 uOffset;
			void main() {
			    gl_Position = vec4(aPos, 0.0, 1.0);
			    vUV = aUV + uOffset;
			}
			""";

			var fragmentCode = versionDef + """

			precision highp float;
			in vec2 vUV;
			out vec4 fragColor;
			uniform sampler2D uTex;
			void main() {
			    fragColor = texture(uTex, vUV);
			}
			""";

			_program = CreateProgram(gl, vertexCode, fragmentCode);
			_uOffsetLoc = gl.GetUniformLocation(_program, "uOffset");
			_uTexLoc = gl.GetUniformLocation(_program, "uTex");
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteTexture(_texture);
			gl.DeleteProgram(_program);
		}

		protected override void RenderOverride(GL gl)
		{
			gl.ClearColor(0.08f, 0.08f, 0.1f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);

			gl.UseProgram(_program);

			// Animate the UV offset so the checkerboard scrolls. Exercises Uniform2f.
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
			gl.Uniform2(_uOffsetLoc, t * 0.25f, t * 0.15f);

			// Bind the texture to unit 0 and tell the sampler uniform to use unit 0.
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, _texture);
			gl.Uniform1(_uTexLoc, 0);

			gl.BindVertexArray(_vao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

			Invalidate();
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
