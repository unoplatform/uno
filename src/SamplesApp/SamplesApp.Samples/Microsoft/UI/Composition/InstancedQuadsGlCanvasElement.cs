#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests:
	//   - Alpha blending: gl.Enable(BLEND), gl.BlendFunc, gl.BlendEquation
	//   - Instanced rendering: gl.DrawArraysInstanced, gl.VertexAttribDivisor
	//   - Multiple vertex buffers (a "per-vertex" VBO and a "per-instance" VBO)
	//   - Uniform4f (per-frame tint)
	public class GLCanvasElement_InstancedQuadsElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int InstanceCount = 24;

		private uint _vao;
		private uint _vertexVbo;
		private uint _instanceVbo;
		private uint _program;
		private int _uTintLoc;
		private DateTime _startTime;

		// Unit quad (two triangles, position only). Will be transformed per instance.
		private static readonly float[] _vertices =
		{
			-0.08f, -0.08f,
			 0.08f, -0.08f,
			 0.08f,  0.08f,
			-0.08f, -0.08f,
			 0.08f,  0.08f,
			-0.08f,  0.08f,
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);

			// Per-vertex VBO (position).
			_vertexVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_vertices), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			// Per-instance VBO: each instance has a vec2 center + vec4 rgba.
			var instances = new float[InstanceCount * 6];
			var rng = new Random(42);
			for (int i = 0; i < InstanceCount; i++)
			{
				instances[i * 6 + 0] = (float)(rng.NextDouble() * 1.6 - 0.8); // x
				instances[i * 6 + 1] = (float)(rng.NextDouble() * 1.6 - 0.8); // y
				instances[i * 6 + 2] = (float)rng.NextDouble();               // r
				instances[i * 6 + 3] = (float)rng.NextDouble();               // g
				instances[i * 6 + 4] = (float)rng.NextDouble();               // b
				instances[i * 6 + 5] = 0.55f;                                 // a (translucent)
			}

			_instanceVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(instances), BufferUsageARB.StaticDraw);

			// Attribute 1: vec2 instance center.
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 6 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(1);
			gl.VertexAttribDivisor(1, 1); // advances once per instance

			// Attribute 2: vec4 instance color.
			gl.VertexAttribPointer(2, 4, GLEnum.Float, false, 6 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(2);
			gl.VertexAttribDivisor(2, 1);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			var vertexCode = versionDef + """

			precision highp float;
			layout(location = 0) in vec2 aPos;
			layout(location = 1) in vec2 aInstanceCenter;
			layout(location = 2) in vec4 aInstanceColor;
			out vec4 vColor;
			void main() {
			    gl_Position = vec4(aPos + aInstanceCenter, 0.0, 1.0);
			    vColor = aInstanceColor;
			}
			""";

			var fragmentCode = versionDef + """

			precision highp float;
			in vec4 vColor;
			out vec4 fragColor;
			uniform vec4 uTint;
			void main() {
			    fragColor = vColor * uTint;
			}
			""";

			_program = CreateProgram(gl, vertexCode, fragmentCode);
			_uTintLoc = gl.GetUniformLocation(_program, "uTint");
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vertexVbo);
			gl.DeleteBuffer(_instanceVbo);
			gl.DeleteProgram(_program);
		}

		protected override void RenderOverride(GL gl)
		{
			gl.ClearColor(0.05f, 0.05f, 0.07f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);

			gl.Enable(EnableCap.Blend);
			gl.BlendEquation(GLEnum.FuncAdd);
			gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

			gl.UseProgram(_program);

			// Pulsing tint via Uniform4f.
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
			var pulse = 0.7f + 0.3f * MathF.Sin(t * 1.5f);
			gl.Uniform4(_uTintLoc, pulse, pulse, pulse, 1f);

			gl.BindVertexArray(_vao);
			gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, InstanceCount);

			gl.Disable(EnableCap.Blend);

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
