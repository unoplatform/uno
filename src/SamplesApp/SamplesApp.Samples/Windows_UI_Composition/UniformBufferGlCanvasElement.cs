#if __SKIA__ || WINAPPSDK
using System;
using System.Runtime.InteropServices;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests Uniform Buffer Objects (WebGL2):
	//   - gl.GenBuffer + gl.BindBuffer(UNIFORM_BUFFER) + gl.BufferData
	//   - gl.GetUniformBlockIndex + gl.UniformBlockBinding
	//   - gl.BindBufferBase (UNIFORM_BUFFER, binding-point, buffer)
	//   - gl.BufferSubData to update the UBO data per frame
	//
	// Renders two quads, each driven by a separate uniform block: one for "scene" data
	// (time + viewport scale) shared across both draws, one for per-quad transform data
	// updated each frame.
	public class GLCanvasElement_UniformBufferElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int SceneBindingPoint = 0;
		private const int TransformBindingPoint = 1;

		// std140 alignment rules: float=4, vec2=8, vec3/vec4=16, block size rounded up to 16.
		// Scene { float uTime; vec2 uScale; } -> 16 bytes: Time @0, pad @4, ScaleX @8, ScaleY @12.
		[StructLayout(LayoutKind.Sequential)]
		private struct SceneBlock
		{
			public float Time;
			public float _pad0;
			public float ScaleX;
			public float ScaleY;
		}

		// Transform { vec2 uTranslate; float uRotation; float uSize; vec3 uColor; } -> 32 bytes.
		// vec3 has 16-byte alignment, so uColor lands at offset 16 with the last 4 bytes padded.
		[StructLayout(LayoutKind.Sequential)]
		private struct TransformBlock
		{
			public float TranslateX; public float TranslateY; public float Rotation; public float Size;
			public float ColorR; public float ColorG; public float ColorB; public float _pad;
		}

		private uint _vao, _vbo, _program;
		private uint _sceneUbo, _transformUbo;
		private DateTime _startTime;

		private static readonly float[] _quad =
		{
			-1f, -1f,
			 1f, -1f,
			 1f,  1f,
			-1f, -1f,
			 1f,  1f,
			-1f,  1f,
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_quad), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			_program = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				layout(std140) uniform Scene {
				    float uTime;
				    vec2 uScale;
				} scene;
				layout(std140) uniform Transform {
				    vec2 uTranslate;
				    float uRotation;
				    float uSize;
				    vec3 uColor;
				} transform;
				out vec3 vColor;
				void main() {
				    float c = cos(transform.uRotation), s = sin(transform.uRotation);
				    mat2 rot = mat2(c, -s, s, c);
				    vec2 p = rot * (aPos * transform.uSize) + transform.uTranslate;
				    gl_Position = vec4(p * scene.uScale, 0.0, 1.0);
				    // Subtle time-based tint mix via the shared Scene block.
				    vColor = transform.uColor * (0.7 + 0.3 * sin(scene.uTime + transform.uTranslate.x * 4.0));
				}
				""",
				versionDef + """

				precision highp float;
				in vec3 vColor;
				out vec4 fragColor;
				void main() { fragColor = vec4(vColor, 1.0); }
				""");

			// Hook up the named uniform blocks to fixed binding points.
			var sceneIdx = gl.GetUniformBlockIndex(_program, "Scene");
			var transformIdx = gl.GetUniformBlockIndex(_program, "Transform");
			gl.UniformBlockBinding(_program, sceneIdx, SceneBindingPoint);
			gl.UniformBlockBinding(_program, transformIdx, TransformBindingPoint);

			// Create the two UBOs with their initial size (DYNAMIC_DRAW: we'll update each frame).
			_sceneUbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.UniformBuffer, _sceneUbo);
			gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)Marshal.SizeOf<SceneBlock>(), null, BufferUsageARB.DynamicDraw);
			gl.BindBufferBase(BufferTargetARB.UniformBuffer, SceneBindingPoint, _sceneUbo);

			_transformUbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.UniformBuffer, _transformUbo);
			gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)Marshal.SizeOf<TransformBlock>(), null, BufferUsageARB.DynamicDraw);
			gl.BindBufferBase(BufferTargetARB.UniformBuffer, TransformBindingPoint, _transformUbo);
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteBuffer(_sceneUbo);
			gl.DeleteBuffer(_transformUbo);
			gl.DeleteProgram(_program);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			gl.ClearColor(0.06f, 0.06f, 0.09f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);

			gl.UseProgram(_program);
			gl.BindVertexArray(_vao);

			// --- Update the shared Scene UBO once per frame.
			var scene = new SceneBlock { Time = t, ScaleX = 0.5f, ScaleY = 0.5f };
			gl.BindBuffer(BufferTargetARB.UniformBuffer, _sceneUbo);
			gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)Marshal.SizeOf<SceneBlock>(), &scene);

			// --- Draw 1: left quad, rotating counter-clockwise, magenta.
			var transformA = new TransformBlock
			{
				TranslateX = -0.9f,
				TranslateY = 0f,
				Rotation = t * 1.2f,
				Size = 0.35f,
				ColorR = 0.95f, ColorG = 0.3f, ColorB = 0.7f,
			};
			gl.BindBuffer(BufferTargetARB.UniformBuffer, _transformUbo);
			gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)Marshal.SizeOf<TransformBlock>(), &transformA);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

			// --- Draw 2: right quad, rotating the other way, cyan.
			var transformB = new TransformBlock
			{
				TranslateX = 0.9f,
				TranslateY = 0f,
				Rotation = -t * 0.8f,
				Size = 0.35f,
				ColorR = 0.3f, ColorG = 0.85f, ColorB = 0.95f,
			};
			gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)Marshal.SizeOf<TransformBlock>(), &transformB);
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
