#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests stencil-buffer operations:
	//   - gl.Enable/Disable(STENCIL_TEST)
	//   - gl.ClearStencil + gl.Clear(STENCIL_BUFFER_BIT)
	//   - gl.StencilFunc, gl.StencilOp, gl.StencilMask
	//   - gl.ColorMask to disable color writes during the mask pass
	//
	// Pass 1: writes 1 into the stencil where a moving circle is drawn (color writes disabled).
	// Pass 2: draws a gradient fullscreen quad only where stencil == 1.
	public class GLCanvasElement_StencilMaskElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int CircleSegments = 64;

		private uint _maskVao, _maskVbo, _maskProgram;
		private int _maskUOffsetLoc;
		private uint _fillVao, _fillVbo, _fillProgram;
		private int _fillUTimeLoc;
		private DateTime _startTime;

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			// Circle as a triangle fan: center vertex + (segments+1) perimeter vertices.
			var maskVerts = new float[(CircleSegments + 2) * 2];
			maskVerts[0] = 0f; maskVerts[1] = 0f;
			for (int i = 0; i <= CircleSegments; i++)
			{
				var angle = i * 2f * MathF.PI / CircleSegments;
				maskVerts[2 + i * 2 + 0] = 0.35f * MathF.Cos(angle);
				maskVerts[2 + i * 2 + 1] = 0.35f * MathF.Sin(angle);
			}

			_maskVao = gl.GenVertexArray();
			gl.BindVertexArray(_maskVao);
			_maskVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _maskVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(maskVerts), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			// Fullscreen quad for the fill pass.
			var quadVerts = new float[]
			{
				-1f, -1f,
				 1f, -1f,
				 1f,  1f,
				-1f, -1f,
				 1f,  1f,
				-1f,  1f,
			};
			_fillVao = gl.GenVertexArray();
			gl.BindVertexArray(_fillVao);
			_fillVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _fillVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(quadVerts), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			_maskProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				uniform vec2 uOffset;
				void main() { gl_Position = vec4(aPos + uOffset, 0.0, 1.0); }
				""",
				versionDef + """

				precision highp float;
				out vec4 fragColor;
				// Color writes are masked off during this pass, so the output doesn't matter.
				void main() { fragColor = vec4(1.0); }
				""");
			_maskUOffsetLoc = gl.GetUniformLocation(_maskProgram, "uOffset");

			_fillProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				out vec2 vNDC;
				void main() {
				    gl_Position = vec4(aPos, 0.0, 1.0);
				    vNDC = aPos;
				}
				""",
				versionDef + """

				precision highp float;
				in vec2 vNDC;
				out vec4 fragColor;
				uniform float uTime;
				void main() {
				    float r = 0.5 + 0.5 * sin(vNDC.x * 6.0 + uTime);
				    float g = 0.5 + 0.5 * sin(vNDC.y * 6.0 + uTime * 1.3);
				    float b = 0.5 + 0.5 * sin((vNDC.x + vNDC.y) * 4.0 - uTime);
				    fragColor = vec4(r, g, b, 1.0);
				}
				""");
			_fillUTimeLoc = gl.GetUniformLocation(_fillProgram, "uTime");
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_maskVao);
			gl.DeleteBuffer(_maskVbo);
			gl.DeleteProgram(_maskProgram);
			gl.DeleteVertexArray(_fillVao);
			gl.DeleteBuffer(_fillVbo);
			gl.DeleteProgram(_fillProgram);
		}

		protected override void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			gl.ClearColor(0.05f, 0.05f, 0.08f, 1f);
			gl.ClearStencil(0);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);

			gl.Enable(EnableCap.StencilTest);

			// --- Pass 1: write 1 into stencil where the circle is, with color writes disabled.
			gl.StencilFunc(StencilFunction.Always, 1, 0xFF);
			gl.StencilMask(0xFF);
			gl.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
			gl.ColorMask(false, false, false, false);

			gl.UseProgram(_maskProgram);
			// Animate the mask position.
			var offsetX = 0.4f * MathF.Cos(t * 0.7f);
			var offsetY = 0.4f * MathF.Sin(t * 0.5f);
			gl.Uniform2(_maskUOffsetLoc, offsetX, offsetY);
			gl.BindVertexArray(_maskVao);
			gl.DrawArrays(PrimitiveType.TriangleFan, 0, CircleSegments + 2);

			// --- Pass 2: draw the gradient fill only where stencil == 1.
			gl.StencilFunc(StencilFunction.Equal, 1, 0xFF);
			gl.StencilMask(0);
			gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
			gl.ColorMask(true, true, true, true);

			gl.UseProgram(_fillProgram);
			gl.Uniform1(_fillUTimeLoc, t);
			gl.BindVertexArray(_fillVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

			gl.Disable(EnableCap.StencilTest);
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
