#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests render-to-texture / multi-pass rendering:
	//   - Custom offscreen FBO with a color-texture attachment
	//   - gl.FramebufferTexture2D, gl.CheckFramebufferStatus
	//   - GL_FRAMEBUFFER_BINDING query to remember the framework's FBO
	//   - Multi-pass dispatch (offscreen draw -> sample as texture -> final composite)
	//   - Sampler binding + Uniform1i for the texture unit
	//   - Wavy distortion uniform animated over time
	public class GLCanvasElement_PostProcessElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int OffscreenSize = 256;

		// Offscreen target
		private uint _offscreenFbo;
		private uint _offscreenColor;

		// Pass 1: rotating colored triangle.
		private uint _sceneVao, _sceneVbo, _sceneProgram;
		private int _sceneUTimeLoc;

		// Pass 2: fullscreen quad sampling the offscreen texture with sine-wave displacement.
		private uint _postVao, _postVbo, _postProgram;
		private int _postUTexLoc, _postUTimeLoc;

		private DateTime _startTime;

		private static readonly float[] _triangleData =
		{
			// pos             // color
			 0.0f,  0.7f,      1f, 0.2f, 0.4f,
			-0.7f, -0.5f,      0.2f, 0.9f, 0.4f,
			 0.7f, -0.5f,      0.3f, 0.5f, 1f,
		};

		private static readonly float[] _quadData =
		{
			// pos        // uv
			-1f, -1f,     0f, 0f,
			 1f, -1f,     1f, 0f,
			 1f,  1f,     1f, 1f,
			-1f, -1f,     0f, 0f,
			 1f,  1f,     1f, 1f,
			-1f,  1f,     0f, 1f,
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			// --- Offscreen color texture ---
			_offscreenColor = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, _offscreenColor);
			gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, OffscreenSize, OffscreenSize, 0, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)0);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (uint)GLEnum.ClampToEdge);

			// --- Offscreen FBO ---
			_offscreenFbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, _offscreenFbo);
			gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, _offscreenColor, 0);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("Offscreen FBO is not complete");
			}

			// --- Scene VAO/VBO ---
			_sceneVao = gl.GenVertexArray();
			gl.BindVertexArray(_sceneVao);
			_sceneVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _sceneVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_triangleData), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 5 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 5 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);

			// --- Post-process VAO/VBO ---
			_postVao = gl.GenVertexArray();
			gl.BindVertexArray(_postVao);
			_postVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _postVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_quadData), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);

			// --- Shaders ---
			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			_sceneProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				layout(location = 1) in vec3 aColor;
				out vec3 vColor;
				uniform float uTime;
				void main() {
				    float c = cos(uTime), s = sin(uTime);
				    mat2 rot = mat2(c, -s, s, c);
				    gl_Position = vec4(rot * aPos, 0.0, 1.0);
				    vColor = aColor;
				}
				""",
				versionDef + """

				precision highp float;
				in vec3 vColor;
				out vec4 fragColor;
				void main() { fragColor = vec4(vColor, 1.0); }
				""");
			_sceneUTimeLoc = gl.GetUniformLocation(_sceneProgram, "uTime");

			_postProgram = CreateProgram(gl,
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
				uniform float uTime;
				void main() {
				    float wave = sin(vUV.y * 24.0 + uTime * 3.0) * 0.03;
				    vec2 sampleUV = vUV + vec2(wave, 0.0);
				    fragColor = texture(uTex, sampleUV);
				}
				""");
			_postUTexLoc = gl.GetUniformLocation(_postProgram, "uTex");
			_postUTimeLoc = gl.GetUniformLocation(_postProgram, "uTime");
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteFramebuffer(_offscreenFbo);
			gl.DeleteTexture(_offscreenColor);
			gl.DeleteVertexArray(_sceneVao);
			gl.DeleteBuffer(_sceneVbo);
			gl.DeleteProgram(_sceneProgram);
			gl.DeleteVertexArray(_postVao);
			gl.DeleteBuffer(_postVbo);
			gl.DeleteProgram(_postProgram);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			// Remember the framework's FBO so we can restore it for the final composite.
			gl.GetInteger(GLEnum.FramebufferBinding, out int frameworkFbo);

			// --- Pass 1: render rotating triangle to offscreen target ---
			gl.BindFramebuffer(GLEnum.Framebuffer, _offscreenFbo);
			gl.Viewport(0, 0, OffscreenSize, OffscreenSize);
			gl.ClearColor(0.08f, 0.1f, 0.2f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_sceneProgram);
			gl.Uniform1(_sceneUTimeLoc, t * 1.2f);
			gl.BindVertexArray(_sceneVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			// --- Pass 2: final composite with displacement sampling the offscreen texture ---
			gl.BindFramebuffer(GLEnum.Framebuffer, (uint)frameworkFbo);
			gl.Viewport(0, 0, (uint)RenderSize.Width, (uint)RenderSize.Height);
			gl.ClearColor(0f, 0f, 0f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_postProgram);
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, _offscreenColor);
			gl.Uniform1(_postUTexLoc, 0);
			gl.Uniform1(_postUTimeLoc, t);
			gl.BindVertexArray(_postVao);
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
