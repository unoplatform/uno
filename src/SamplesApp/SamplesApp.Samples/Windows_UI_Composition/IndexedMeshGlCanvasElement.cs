#if __SKIA__ || WINAPPSDK
using System;
using System.Numerics;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests indexed drawing and fixed-function raster state:
	//   - ELEMENT_ARRAY_BUFFER captured by the VAO + gl.DrawElements
	//   - gl.GetAttribLocation (attribute located by name, not layout qualifier)
	//   - gl.DepthFunc / gl.DepthMask (explicit depth configuration)
	//   - gl.Enable(CULL_FACE) + gl.CullFace + gl.FrontFace
	//   - gl.Enable(SCISSOR_TEST) + gl.Scissor (split background clears)
	//   - gl.LineWidth + a LINES index buffer for the wireframe overlay
	//   - gl.DisableVertexAttribArray (constant attribute for the wireframe pass)
	//
	// Renders a spinning indexed cube (8 vertices, 36 indices) over a two-tone scissored
	// background, with black wireframe edges drawn from a second index buffer.
	public class GLCanvasElement_IndexedMeshElement() : GLCanvasElement(() => App.MainWindow)
	{
		private uint _vao, _vbo, _triEbo, _lineEbo;
		private uint _program;
		private int _uMvpLoc;
		private uint _colorAttribLoc;
		private DateTime _startTime;

		// 8 cube corners: position (xyz) + color (rgb).
		private static readonly float[] _vertices =
		{
			-1, -1, -1,   0.2f, 0.3f, 0.8f,
			 1, -1, -1,   0.9f, 0.3f, 0.2f,
			 1,  1, -1,   0.2f, 0.8f, 0.3f,
			-1,  1, -1,   0.9f, 0.8f, 0.2f,
			-1, -1,  1,   0.8f, 0.2f, 0.8f,
			 1, -1,  1,   0.2f, 0.8f, 0.8f,
			 1,  1,  1,   0.9f, 0.9f, 0.9f,
			-1,  1,  1,   0.4f, 0.4f, 0.4f,
		};

		// 12 triangles, CCW as seen from outside (for culling).
		private static readonly ushort[] _triIndices =
		{
			0, 2, 1, 0, 3, 2, // -Z
			4, 5, 6, 4, 6, 7, // +Z
			0, 1, 5, 0, 5, 4, // -Y
			3, 6, 2, 3, 7, 6, // +Y
			0, 4, 7, 0, 7, 3, // -X
			1, 2, 6, 1, 6, 5, // +X
		};

		// 12 edges as LINES.
		private static readonly ushort[] _lineIndices =
		{
			0, 1, 1, 2, 2, 3, 3, 0,
			4, 5, 5, 6, 6, 7, 7, 4,
			0, 4, 1, 5, 2, 6, 3, 7,
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			// aColor intentionally has NO layout qualifier; its location is queried by name.
			_program = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec3 aPos;
				in vec3 aColor;
				uniform mat4 uMvp;
				out vec3 vColor;
				void main() {
				    gl_Position = uMvp * vec4(aPos, 1.0);
				    vColor = aColor;
				}
				""",
				versionDef + """

				precision highp float;
				in vec3 vColor;
				out vec4 fragColor;
				void main() { fragColor = vec4(vColor, 1.0); }
				""");
			_uMvpLoc = gl.GetUniformLocation(_program, "uMvp");
			var colorLoc = gl.GetAttribLocation(_program, "aColor");
			if (colorLoc < 0)
			{
				throw new Exception("GetAttribLocation(aColor) failed");
			}
			_colorAttribLoc = (uint)colorLoc;

			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_vertices), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(_colorAttribLoc, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
			gl.EnableVertexAttribArray(_colorAttribLoc);
			// The ELEMENT_ARRAY_BUFFER binding is part of VAO state.
			_triEbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _triEbo);
			gl.BufferData(BufferTargetARB.ElementArrayBuffer, new ReadOnlySpan<ushort>(_triIndices), BufferUsageARB.StaticDraw);
			gl.BindVertexArray(0);

			_lineEbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _lineEbo);
			gl.BufferData(BufferTargetARB.ElementArrayBuffer, new ReadOnlySpan<ushort>(_lineIndices), BufferUsageARB.StaticDraw);
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteBuffer(_triEbo);
			gl.DeleteBuffer(_lineEbo);
			gl.DeleteProgram(_program);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
			var w = (int)RenderSize.Width;
			var h = (int)RenderSize.Height;

			// --- Two-tone background via scissored clears ---
			gl.Enable(EnableCap.ScissorTest);
			gl.Scissor(0, 0, (uint)(w / 2), (uint)h);
			gl.ClearColor(0.10f, 0.05f, 0.05f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.Scissor(w / 2, 0, (uint)(w - w / 2), (uint)h);
			gl.ClearColor(0.05f, 0.05f, 0.10f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.Disable(EnableCap.ScissorTest);
			gl.Clear(ClearBufferMask.DepthBufferBit);

			// --- Depth + culling state ---
			gl.Enable(EnableCap.DepthTest);
			gl.DepthFunc(DepthFunction.Lequal);
			gl.DepthMask(true);
			// Silk.NET's double-precision desktop names; aliased to the f-variants on WebGL2.
			gl.ClearDepth(1.0);
			gl.DepthRange(0.0, 1.0);
			gl.Enable(EnableCap.CullFace);
			gl.CullFace(CullFaceMode.Back);
			gl.FrontFace(FrontFaceDirection.Ccw);

			var mvp =
				Matrix4x4.CreateRotationY(t * 0.7f) *
				Matrix4x4.CreateRotationX(t * 0.4f) *
				Matrix4x4.CreateTranslation(0, 0, -5f) *
				SimplePerspective(0.7f);

			gl.UseProgram(_program);
			gl.UniformMatrix4(_uMvpLoc, 1, false, (float*)&mvp);
			gl.BindVertexArray(_vao);

			// --- Solid cube via the VAO-captured triangle EBO ---
			gl.DrawElements(PrimitiveType.Triangles, (uint)_triIndices.Length, DrawElementsType.UnsignedShort, (void*)0);

			// --- Wireframe overlay: swap in the LINES EBO and use a constant black color ---
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _lineEbo);
			gl.DisableVertexAttribArray(_colorAttribLoc);
			gl.VertexAttrib3(_colorAttribLoc, 0f, 0f, 0f);
			gl.LineWidth(2f);
			gl.DepthFunc(DepthFunction.Always);
			gl.DrawElements(PrimitiveType.Lines, (uint)_lineIndices.Length, DrawElementsType.UnsignedShort, (void*)0);
			gl.LineWidth(1f);
			// Restore the VAO's own EBO/attribute state for the next frame.
			gl.EnableVertexAttribArray(_colorAttribLoc);
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _triEbo);

			gl.DepthFunc(DepthFunction.Less);
			gl.Disable(EnableCap.CullFace);
			gl.Disable(EnableCap.DepthTest);

			Invalidate();
		}

		private static Matrix4x4 SimplePerspective(float scale)
		{
			const float n = 0.5f, f = 16f;
			return new Matrix4x4(
				n / scale, 0, 0, 0,
				0, n / scale, 0, 0,
				0, 0, (-f - n) / (f - n), -1,
				0, 0, (2 * f * n) / (n - f), 0);
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
