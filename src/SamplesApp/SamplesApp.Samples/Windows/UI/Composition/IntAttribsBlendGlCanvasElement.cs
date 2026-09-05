#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests integer vertex attributes, constant (disabled-array) attributes, and
	// the "separate" blend/stencil state:
	//   - gl.VertexAttribIPointer with an ivec4 per-instance attribute
	//   - constant attributes via gl.VertexAttrib4 (f + fv forms, alternating per frame) and
	//     gl.VertexAttribI4 (i/iv and ui/uiv forms)
	//   - gl.GetVertexAttrib (iv/fv) + gl.GetVertexAttribI (iv/uiv) + gl.GetVertexAttribPointer
	//   - gl.DrawElementsInstanced + gl.DrawRangeElements
	//   - gl.BlendColor, gl.BlendFuncSeparate, gl.BlendEquationSeparate
	//   - gl.StencilFuncSeparate / gl.StencilOpSeparate / gl.StencilMaskSeparate
	//   - gl.PolygonOffset (two coplanar quads resolved by offset)
	//   - gl.SampleCoverage
	//
	// Renders an instanced grid of translucent tinted quads (positions derived from the integer
	// per-instance attribute) over a gradient, with one stencil-masked highlight cell and a
	// coplanar pair demonstrating polygon offset.
	public class GLCanvasElement_IntAttribsBlendElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int GridSize = 4;
		private const int InstanceCount = GridSize * GridSize;

		private uint _vao, _quadVbo, _instanceVbo, _ebo, _program;
		private int _uTimeLoc;
		private DateTime _startTime;
		private bool _evenFrame;

		private static readonly float[] _quadVertices = { 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f };
		private static readonly ushort[] _quadIndices = { 0, 1, 2, 0, 2, 3 };

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			_program = CreateProgram(gl,
				versionDef + """

				precision highp float;
				precision highp int;
				layout(location = 0) in vec2 aPos;
				layout(location = 1) in ivec4 aCell;     // (col, row, paletteIndex, unused)
				layout(location = 2) in vec4 aTint;      // constant attribute (f/fv)
				layout(location = 3) in uvec4 aExtra;    // constant attribute (ui/uiv)
				layout(location = 4) in ivec4 aBias;     // constant attribute (i/iv)
				uniform float uTime;
				out vec4 vColor;
				void main() {
				    float cells = 4.0;
				    vec2 cell = vec2(float(aCell.x), float(aCell.y));
				    vec2 origin = (cell / cells) * 1.6 - 0.8;
				    float wob = 0.04 * sin(uTime * 2.0 + float(aCell.x + aCell.y));
				    vec2 p = origin + aPos * (1.4 / cells) + vec2(wob);
				    gl_Position = vec4(p, 0.5, 1.0);
				    float palette = float(aCell.z) / 4.0;
				    vColor = vec4(palette, 1.0 - palette, 0.6, 0.55) * aTint
				        + vec4((float(aExtra.x) + float(aBias.x)) * 0.0001);
				}
				""",
				versionDef + """

				precision highp float;
				in vec4 vColor;
				out vec4 fragColor;
				void main() { fragColor = vColor; }
				""");
			_uTimeLoc = gl.GetUniformLocation(_program, "uTime");

			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);

			_quadVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_quadVertices), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			// Integer per-instance attribute: must use VertexAttribIPointer (not the float variant).
			var cells = new int[InstanceCount * 4];
			for (int i = 0; i < InstanceCount; i++)
			{
				cells[i * 4 + 0] = i % GridSize;
				cells[i * 4 + 1] = i / GridSize;
				cells[i * 4 + 2] = i % 5;
				cells[i * 4 + 3] = 0;
			}
			_instanceVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<int>(cells), BufferUsageARB.StaticDraw);
			gl.VertexAttribIPointer(1, 4, GLEnum.Int, 4 * sizeof(int), (void*)0);
			gl.EnableVertexAttribArray(1);
			gl.VertexAttribDivisor(1, 1);

			_ebo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
			gl.BufferData(BufferTargetARB.ElementArrayBuffer, new ReadOnlySpan<ushort>(_quadIndices), BufferUsageARB.StaticDraw);
			gl.BindVertexArray(0);

			// Constant attributes; the I-variants are exercised here, the f-variants per frame.
			gl.VertexAttribI4(3, 1u, 2u, 3u, 4u);
			gl.VertexAttribI4(4, new ReadOnlySpan<int>(new int[] { 5, 6, 7, 8 }));

			// --- Self-check the attribute state through the GetVertexAttrib* getters ---
			gl.BindVertexArray(_vao);
			gl.GetVertexAttrib(1, GLEnum.VertexAttribArrayEnabled, out int enabled);
			Check(enabled == 1, "GetVertexAttribiv(Enabled) returned 0");
			gl.GetVertexAttrib(1, GLEnum.VertexAttribArraySize, out int size);
			Check(size == 4, $"GetVertexAttribiv(Size) returned {size}");
			gl.GetVertexAttrib(1, GLEnum.VertexAttribArrayDivisor, out int divisor);
			Check(divisor == 1, $"GetVertexAttribiv(Divisor) returned {divisor}");
			gl.GetVertexAttribPointer(1, GLEnum.VertexAttribArrayPointer, out void* pointer);
			Check((nint)pointer == 0, $"GetVertexAttribPointerv returned {(nint)pointer}");
			gl.BindVertexArray(0);

			gl.VertexAttrib4(2, 1f, 1f, 1f, 1f);
			var tint = stackalloc float[4];
			gl.GetVertexAttrib(2, GLEnum.CurrentVertexAttrib, tint);
			Check(Math.Abs(tint[0] - 1f) < 0.001f, $"GetVertexAttribfv(CurrentVertexAttrib) returned {tint[0]}");

			var extra = stackalloc uint[4];
			gl.GetVertexAttribI(3, GLEnum.CurrentVertexAttrib, extra);
			Check(extra[3] == 4u, $"GetVertexAttribIuiv returned {extra[3]}");
			var bias = stackalloc int[4];
			gl.GetVertexAttribI(4, GLEnum.CurrentVertexAttrib, bias);
			Check(bias[0] == 5, $"GetVertexAttribIiv returned {bias[0]}");

			var err = gl.GetError();
			if (err != GLEnum.NoError)
			{
				throw new Exception($"GL error at end of Init: 0x{(int)err:x}");
			}
		}

		private static void Check(bool condition, string message)
		{
			if (!condition)
			{
				throw new Exception("IntAttribsBlend check failed: " + message);
			}
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_quadVbo);
			gl.DeleteBuffer(_instanceVbo);
			gl.DeleteBuffer(_ebo);
			gl.DeleteProgram(_program);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
			_evenFrame = !_evenFrame;

			gl.ClearColor(0.08f, 0.07f, 0.1f, 1f);
			gl.ClearStencil(0);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			gl.UseProgram(_program);
			gl.Uniform1(_uTimeLoc, t);

			// Alternate between the scalar and the array form of the constant tint attribute.
			if (_evenFrame)
			{
				gl.VertexAttrib4(2, 1f, 1f, 1f, 1f);
			}
			else
			{
				gl.VertexAttrib4(2, new ReadOnlySpan<float>(new float[] { 0.9f, 0.95f, 1f, 1f }));
			}

			// --- Blended instanced grid with "separate" blend state ---
			gl.Enable(EnableCap.Blend);
			gl.BlendColor(0.2f, 0.2f, 0.2f, 0.5f);
			gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusConstantAlpha);
			gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.Max);
			gl.SampleCoverage(0.9f, false);

			// Stencil via the Separate entry points. Note: WebGL2 requires the reference and
			// masks to be identical for front and back faces (unlike desktop GL); only the
			// ops may differ, so the variation is in StencilOpSeparate.
			gl.Enable(EnableCap.StencilTest);
			gl.StencilFuncSeparate(GLEnum.Front, GLEnum.Always, 1, 0xFF);
			gl.StencilFuncSeparate(GLEnum.Back, GLEnum.Always, 1, 0xFF);
			gl.StencilMaskSeparate(GLEnum.Front, 0xFF);
			gl.StencilMaskSeparate(GLEnum.Back, 0xFF);
			gl.StencilOpSeparate(GLEnum.Front, GLEnum.Keep, GLEnum.Keep, GLEnum.Replace);
			gl.StencilOpSeparate(GLEnum.Back, GLEnum.Keep, GLEnum.Keep, GLEnum.Keep);

			gl.BindVertexArray(_vao);
			gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)_quadIndices.Length, DrawElementsType.UnsignedShort, (void*)0, InstanceCount);

			// --- Coplanar highlight pair: same depth, resolved via PolygonOffset ---
			gl.Enable(EnableCap.DepthTest);
			gl.DepthFunc(DepthFunction.Lequal);
			// Base quad: only where the stencil was written (over the grid).
			gl.StencilFuncSeparate(GLEnum.FrontAndBack, GLEnum.Equal, 1, 0xFF);
			gl.StencilMaskSeparate(GLEnum.FrontAndBack, 0);
			gl.VertexAttribI4(1, 1, 1, 4, 0); // constant aCell: draw a single cell quad
			gl.DisableVertexAttribArray(1);
			gl.BindVertexArray(_vao);
			gl.DrawRangeElements(PrimitiveType.Triangles, 0, 3, (uint)_quadIndices.Length, DrawElementsType.UnsignedShort, (void*)0);
			// Offset twin: drawn at the same depth but pushed back, so the base quad wins cleanly.
			gl.Enable(EnableCap.PolygonOffsetFill);
			gl.PolygonOffset(1f, 1f);
			gl.VertexAttribI4(1, 2, 1, 0, 0);
			gl.DrawRangeElements(PrimitiveType.Triangles, 0, 3, (uint)_quadIndices.Length, DrawElementsType.UnsignedShort, (void*)0);
			gl.Disable(EnableCap.PolygonOffsetFill);

			// Restore the per-instance array attribute and global state.
			gl.BindVertexArray(_vao);
			gl.EnableVertexAttribArray(1);
			gl.BindVertexArray(0);
			gl.Disable(EnableCap.DepthTest);
			gl.Disable(EnableCap.StencilTest);
			gl.StencilMaskSeparate(GLEnum.FrontAndBack, 0xFF);
			gl.Disable(EnableCap.Blend);
			gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.FuncAdd);
			gl.SampleCoverage(1f, false);

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
