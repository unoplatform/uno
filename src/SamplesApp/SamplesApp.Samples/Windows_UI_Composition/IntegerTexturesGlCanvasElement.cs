#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests integer texture formats and the integer ClearBuffer variants:
	//   - TexStorage2D with RGBA8UI (unsigned) and RGBA8I (signed) internal formats
	//   - rendering INTO integer color attachments (uvec4 / ivec4 fragment outputs)
	//   - gl.ClearBuffer with uint values (glClearBufferuiv) and int values (glClearBufferiv)
	//     on color attachments
	//   - sampling via usampler2D / isampler2D (NEAREST only - integer formats are not
	//     filterable, and blending must stay disabled while drawing into them)
	//
	// Left half: the unsigned target; right half: the signed target. Each is cleared to a
	// time-cycling integer color and overdrawn with a spinning triangle whose integer outputs
	// encode its vertex colors, then visualized by normalizing the integer texels to floats.
	public class GLCanvasElement_IntegerTexturesElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int Size = 128;

		private uint _uintTex, _uintFbo, _intTex, _intFbo;
		private uint _triVao, _triVbo, _quadVao, _quadVbo;
		private uint _uintSceneProgram, _intSceneProgram, _compositeProgram;
		private int _uintSceneUTimeLoc, _intSceneUTimeLoc;
		private int _uUTexLoc, _uITexLoc;
		private DateTime _startTime;

		private static readonly float[] _triangleData =
		{
			// pos             // color
			 0.0f,  0.8f,      1f, 0.2f, 0.3f,
			-0.8f, -0.6f,      0.2f, 1f, 0.4f,
			 0.8f, -0.6f,      0.3f, 0.4f, 1f,
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

			// --- Integer color targets. NEAREST is mandatory: integer formats are unfilterable.
			_uintTex = MakeIntegerTexture(gl, SizedInternalFormat.Rgba8ui);
			_uintFbo = MakeFbo(gl, _uintTex);
			_intTex = MakeIntegerTexture(gl, SizedInternalFormat.Rgba8i);
			_intFbo = MakeFbo(gl, _intTex);

			// --- Geometry ---
			_triVao = gl.GenVertexArray();
			gl.BindVertexArray(_triVao);
			_triVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _triVbo);
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
			var isEs = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase);
			var versionDef = isEs ? "#version 300 es" : "#version 330";
			var samplerPrecision = isEs ? "precision highp usampler2D;\nprecision highp isampler2D;" : "";

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

			_uintSceneProgram = CreateProgram(gl, sceneVs,
				versionDef + """

				precision highp float;
				in vec3 vColor;
				layout(location = 0) out uvec4 o;
				void main() { o = uvec4(uvec3(vColor * 255.0), 255u); }
				""");
			_uintSceneUTimeLoc = gl.GetUniformLocation(_uintSceneProgram, "uTime");

			_intSceneProgram = CreateProgram(gl, sceneVs,
				versionDef + """

				precision highp float;
				in vec3 vColor;
				layout(location = 0) out ivec4 o;
				void main() { o = ivec4(ivec3(vColor * 255.0) - 128, 127); }
				""");
			_intSceneUTimeLoc = gl.GetUniformLocation(_intSceneProgram, "uTime");

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
				versionDef + "\n" + samplerPrecision + """

				precision highp float;
				in vec2 vUV;
				out vec4 fragColor;
				uniform usampler2D uUTex;
				uniform isampler2D uITex;
				void main() {
				    if (vUV.x < 0.5) {
				        uvec4 u = texture(uUTex, vec2(vUV.x * 2.0, vUV.y));
				        fragColor = vec4(vec3(u.rgb) / 255.0, 1.0);
				    } else {
				        ivec4 i = texture(uITex, vec2((vUV.x - 0.5) * 2.0, vUV.y));
				        fragColor = vec4((vec3(i.rgb) + 128.0) / 255.0, 1.0);
				    }
				}
				""");
			_uUTexLoc = gl.GetUniformLocation(_compositeProgram, "uUTex");
			_uITexLoc = gl.GetUniformLocation(_compositeProgram, "uITex");
		}

		private static uint MakeIntegerTexture(GL gl, SizedInternalFormat format)
		{
			var tex = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, tex);
			gl.TexStorage2D(TextureTarget.Texture2D, 1, format, Size, Size);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Nearest);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Nearest);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (uint)GLEnum.ClampToEdge);
			gl.BindTexture(TextureTarget.Texture2D, 0);
			return tex;
		}

		private static uint MakeFbo(GL gl, uint colorTex)
		{
			var fbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, fbo);
			gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, colorTex, 0);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("Integer FBO is not complete");
			}
			return fbo;
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteTexture(_uintTex);
			gl.DeleteFramebuffer(_uintFbo);
			gl.DeleteTexture(_intTex);
			gl.DeleteFramebuffer(_intFbo);
			gl.DeleteVertexArray(_triVao);
			gl.DeleteBuffer(_triVbo);
			gl.DeleteVertexArray(_quadVao);
			gl.DeleteBuffer(_quadVbo);
			gl.DeleteProgram(_uintSceneProgram);
			gl.DeleteProgram(_intSceneProgram);
			gl.DeleteProgram(_compositeProgram);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			gl.GetInteger(GLEnum.FramebufferBinding, out int frameworkFbo);

			// --- Unsigned target: glClearBufferuiv + uvec4-output triangle ---
			gl.BindFramebuffer(GLEnum.Framebuffer, _uintFbo);
			gl.Viewport(0, 0, Size, Size);
			var phase = 0.5f + 0.5f * MathF.Sin(t);
			var uClear = stackalloc uint[] { (uint)(40 + 60 * phase), 20, (uint)(80 - 40 * phase), 255 };
			gl.ClearBuffer(GLEnum.Color, 0, uClear);
			gl.UseProgram(_uintSceneProgram);
			gl.Uniform1(_uintSceneUTimeLoc, t);
			gl.BindVertexArray(_triVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			// --- Signed target: glClearBufferiv + ivec4-output triangle ---
			gl.BindFramebuffer(GLEnum.Framebuffer, _intFbo);
			var iClear = stackalloc int[] { (int)(-90 + 60 * phase), -20, (int)(40 * phase), 127 };
			gl.ClearBuffer(GLEnum.Color, 0, iClear);
			gl.UseProgram(_intSceneProgram);
			gl.Uniform1(_intSceneUTimeLoc, -t);
			gl.BindVertexArray(_triVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			// --- Composite: normalize both integer textures to floats ---
			gl.BindFramebuffer(GLEnum.Framebuffer, (uint)frameworkFbo);
			gl.Viewport(0, 0, (uint)RenderSize.Width, (uint)RenderSize.Height);
			gl.ClearColor(0f, 0f, 0f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_compositeProgram);
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, _uintTex);
			gl.ActiveTexture(TextureUnit.Texture1);
			gl.BindTexture(TextureTarget.Texture2D, _intTex);
			gl.Uniform1(_uUTexLoc, 0);
			gl.Uniform1(_uITexLoc, 1);
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
