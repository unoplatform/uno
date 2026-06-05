#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests multiple render targets and framebuffer blitting:
	//   - gl.TexStorage2D (immutable texture storage)
	//   - gl.DrawBuffers + a fragment shader with two outputs (MRT)
	//   - gl.ClearBuffer (per-attachment clears)
	//   - gl.RenderbufferStorageMultisample (MSAA offscreen target)
	//   - gl.BlitFramebuffer (MSAA resolve; routed through the >8-arg C shim on wasm)
	//   - separate READ_FRAMEBUFFER / DRAW_FRAMEBUFFER bindings
	//   - gl.InvalidateFramebuffer (hint after resolve)
	//
	// Renders a spinning triangle to a 2-attachment MRT target (normal + inverted colors) and
	// a second triangle to a 4x MSAA target resolved via blit. The final composite shows the
	// three textures as vertical bands: [MRT color 0 | MRT color 1 | MSAA resolve].
	public class GLCanvasElement_MRTBlitElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int Size = 256;

		// MRT target: two color textures.
		private uint _mrtFbo, _mrtColor0, _mrtColor1;
		// MSAA target + its single-sample resolve target.
		private uint _msaaFbo, _msaaRbo, _resolveFbo, _resolveColor;

		private uint _sceneVao, _sceneVbo;
		private uint _quadVao, _quadVbo;
		private uint _mrtProgram, _plainProgram, _compositeProgram;
		private int _mrtUTimeLoc, _plainUTimeLoc, _compositeUTex0Loc, _compositeUTex1Loc, _compositeUTex2Loc;
		private DateTime _startTime;

		private static readonly float[] _triangleData =
		{
			// pos             // color
			 0.0f,  0.7f,      1f, 0.3f, 0.2f,
			-0.7f, -0.5f,      0.2f, 1f, 0.3f,
			 0.7f, -0.5f,      0.3f, 0.2f, 1f,
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

			// --- MRT target: two immutable-storage color textures on one FBO ---
			_mrtColor0 = MakeStorageTexture(gl);
			_mrtColor1 = MakeStorageTexture(gl);
			_mrtFbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, _mrtFbo);
			gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, _mrtColor0, 0);
			gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment1, GLEnum.Texture2D, _mrtColor1, 0);
			ReadOnlySpan<GLEnum> drawBuffers = stackalloc GLEnum[] { GLEnum.ColorAttachment0, GLEnum.ColorAttachment1 };
			gl.DrawBuffers((uint)drawBuffers.Length, drawBuffers);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("MRT FBO is not complete");
			}

			// --- MSAA target: 4x multisampled renderbuffer ---
			_msaaRbo = gl.GenRenderbuffer();
			gl.BindRenderbuffer(GLEnum.Renderbuffer, _msaaRbo);
			gl.RenderbufferStorageMultisample(GLEnum.Renderbuffer, 4, GLEnum.Rgba8, Size, Size);
			gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);
			_msaaFbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, _msaaFbo);
			gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Renderbuffer, _msaaRbo);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("MSAA FBO is not complete");
			}

			// --- Resolve target: single-sample texture the MSAA target is blitted into ---
			_resolveColor = MakeStorageTexture(gl);
			_resolveFbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, _resolveFbo);
			gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, _resolveColor, 0);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("Resolve FBO is not complete");
			}

			// --- Geometry ---
			_sceneVao = gl.GenVertexArray();
			gl.BindVertexArray(_sceneVao);
			_sceneVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _sceneVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_triangleData), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 5 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 5 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);

			_quadVao = gl.GenVertexArray();
			gl.BindVertexArray(_quadVao);
			_quadVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
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

			var sceneVs = versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				layout(location = 1) in vec3 aColor;
				out vec3 vColor;
				uniform float uTime;
				void main() {
				    float c = cos(uTime), s = sin(uTime);
				    gl_Position = vec4(mat2(c, -s, s, c) * aPos, 0.0, 1.0);
				    vColor = aColor;
				}
				""";

			_mrtProgram = CreateProgram(gl, sceneVs,
				versionDef + """

				precision highp float;
				in vec3 vColor;
				layout(location = 0) out vec4 o0;
				layout(location = 1) out vec4 o1;
				void main() {
				    o0 = vec4(vColor, 1.0);
				    o1 = vec4(1.0 - vColor, 1.0);
				}
				""");
			_mrtUTimeLoc = gl.GetUniformLocation(_mrtProgram, "uTime");

			_plainProgram = CreateProgram(gl, sceneVs,
				versionDef + """

				precision highp float;
				in vec3 vColor;
				out vec4 fragColor;
				void main() { fragColor = vec4(vColor, 1.0); }
				""");
			_plainUTimeLoc = gl.GetUniformLocation(_plainProgram, "uTime");

			_compositeProgram = CreateProgram(gl,
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
				uniform sampler2D uTex0;
				uniform sampler2D uTex1;
				uniform sampler2D uTex2;
				void main() {
				    // Three vertical bands: MRT output 0, MRT output 1, MSAA resolve.
				    vec2 uv = vec2(fract(vUV.x * 3.0), vUV.y);
				    if (vUV.x < 1.0 / 3.0) fragColor = texture(uTex0, uv);
				    else if (vUV.x < 2.0 / 3.0) fragColor = texture(uTex1, uv);
				    else fragColor = texture(uTex2, uv);
				}
				""");
			_compositeUTex0Loc = gl.GetUniformLocation(_compositeProgram, "uTex0");
			_compositeUTex1Loc = gl.GetUniformLocation(_compositeProgram, "uTex1");
			_compositeUTex2Loc = gl.GetUniformLocation(_compositeProgram, "uTex2");
		}

		private static uint MakeStorageTexture(GL gl)
		{
			var tex = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, tex);
			gl.TexStorage2D(TextureTarget.Texture2D, 1, SizedInternalFormat.Rgba8, Size, Size);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (uint)GLEnum.ClampToEdge);
			gl.BindTexture(TextureTarget.Texture2D, 0);
			return tex;
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteFramebuffer(_mrtFbo);
			gl.DeleteTexture(_mrtColor0);
			gl.DeleteTexture(_mrtColor1);
			gl.DeleteFramebuffer(_msaaFbo);
			gl.DeleteRenderbuffer(_msaaRbo);
			gl.DeleteFramebuffer(_resolveFbo);
			gl.DeleteTexture(_resolveColor);
			gl.DeleteVertexArray(_sceneVao);
			gl.DeleteBuffer(_sceneVbo);
			gl.DeleteVertexArray(_quadVao);
			gl.DeleteBuffer(_quadVbo);
			gl.DeleteProgram(_mrtProgram);
			gl.DeleteProgram(_plainProgram);
			gl.DeleteProgram(_compositeProgram);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			gl.GetInteger(GLEnum.FramebufferBinding, out int frameworkFbo);

			// --- Pass A: spinning triangle into the 2-attachment MRT target ---
			gl.BindFramebuffer(GLEnum.Framebuffer, _mrtFbo);
			gl.Viewport(0, 0, Size, Size);
			var clear0 = stackalloc float[] { 0.1f, 0.1f, 0.15f, 1f };
			var clear1 = stackalloc float[] { 0.15f, 0.1f, 0.1f, 1f };
			gl.ClearBuffer(GLEnum.Color, 0, clear0);
			gl.ClearBuffer(GLEnum.Color, 1, clear1);
			gl.UseProgram(_mrtProgram);
			gl.Uniform1(_mrtUTimeLoc, t);
			gl.BindVertexArray(_sceneVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			// --- Pass B: counter-spinning triangle into the MSAA target, then resolve ---
			gl.BindFramebuffer(GLEnum.Framebuffer, _msaaFbo);
			var clearMsaa = stackalloc float[] { 0.1f, 0.15f, 0.1f, 1f };
			gl.ClearBuffer(GLEnum.Color, 0, clearMsaa);
			gl.UseProgram(_plainProgram);
			gl.Uniform1(_plainUTimeLoc, -t);
			gl.BindVertexArray(_sceneVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			gl.BindFramebuffer(GLEnum.ReadFramebuffer, _msaaFbo);
			gl.BindFramebuffer(GLEnum.DrawFramebuffer, _resolveFbo);
			gl.BlitFramebuffer(0, 0, Size, Size, 0, 0, Size, Size, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
			// The MSAA contents are consumed; tell the driver it can drop them.
			ReadOnlySpan<GLEnum> invalidate = stackalloc GLEnum[] { GLEnum.ColorAttachment0 };
			gl.InvalidateFramebuffer(GLEnum.ReadFramebuffer, (uint)invalidate.Length, invalidate);

			// --- Composite: three vertical bands onto the framework target ---
			gl.BindFramebuffer(GLEnum.Framebuffer, (uint)frameworkFbo);
			gl.Viewport(0, 0, (uint)RenderSize.Width, (uint)RenderSize.Height);
			gl.ClearColor(0f, 0f, 0f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_compositeProgram);
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, _mrtColor0);
			gl.ActiveTexture(TextureUnit.Texture1);
			gl.BindTexture(TextureTarget.Texture2D, _mrtColor1);
			gl.ActiveTexture(TextureUnit.Texture2);
			gl.BindTexture(TextureTarget.Texture2D, _resolveColor);
			gl.Uniform1(_compositeUTex0Loc, 0);
			gl.Uniform1(_compositeUTex1Loc, 1);
			gl.Uniform1(_compositeUTex2Loc, 2);
			gl.BindVertexArray(_quadVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
			gl.ActiveTexture(TextureUnit.Texture0);

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
