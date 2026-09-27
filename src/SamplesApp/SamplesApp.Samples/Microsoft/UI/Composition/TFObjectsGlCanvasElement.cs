#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests named transform-feedback objects and the remaining buffer plumbing:
	//   - gl.GenTransformFeedbacks / gl.BindTransformFeedback / gl.IsTransformFeedback /
	//     gl.DeleteTransformFeedbacks (TF buffer bindings live in the TF object)
	//   - gl.BindBufferRange (instead of BindBufferBase) for the capture buffer
	//   - gl.PauseTransformFeedback / gl.ResumeTransformFeedback with a decoy draw while
	//     paused: if the pause is broken, the decoy is captured and the ring collapses
	//   - gl.GetTransformFeedbackVarying (post-link introspection check)
	//   - gl.CopyBufferSubData (captured buffer -> render buffer via COPY_READ/COPY_WRITE)
	//   - gl.ClearBufferfi (DEPTH_STENCIL) + gl.ClearBufferiv (STENCIL)
	//   - gl.FenceSync + gl.WaitSync (server-side wait between update and render)
	//
	// A ring of points orbits the center: positions are recomputed via transform feedback into a
	// capture buffer each frame (in two halves with a pause in between), copied into a render
	// buffer with CopyBufferSubData, and drawn from there.
	public class GLCanvasElement_TFObjectsElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int PointCount = 256;

		private uint _sourceVbo, _captureVbo, _renderVbo;
		private uint _sourceVao, _renderVao;
		private uint _tf;
		private uint _updateProgram, _renderProgram;
		private int _uTimeLoc;
		private DateTime _startTime;

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			// Source: per-point base angle + radius.
			var source = new float[PointCount * 2];
			var rng = new Random(7);
			for (int i = 0; i < PointCount; i++)
			{
				source[i * 2 + 0] = (float)(i * 2 * Math.PI / PointCount); // angle
				source[i * 2 + 1] = 0.3f + (float)rng.NextDouble() * 0.5f; // radius
			}

			_sourceVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _sourceVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(source), BufferUsageARB.StaticDraw);

			_captureVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _captureVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(PointCount * 2 * sizeof(float)), null, BufferUsageARB.DynamicCopy);

			_renderVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _renderVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(PointCount * 2 * sizeof(float)), null, BufferUsageARB.DynamicDraw);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

			_sourceVao = MakeVao(gl, _sourceVbo);
			_renderVao = MakeVao(gl, _renderVbo);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			// Update program: angle/radius -> orbiting xy, captured as vPos.
			var updateVs = CompileShader(gl, ShaderType.VertexShader, versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPolar; // (angle, radius)
				uniform float uTime;
				out vec2 vPos;
				void main() {
				    float angle = aPolar.x + uTime * (0.2 + aPolar.y * 0.5);
				    vPos = vec2(cos(angle), sin(angle)) * aPolar.y;
				    gl_Position = vec4(vPos, 0.0, 1.0);
				}
				""");
			var updateFs = CompileShader(gl, ShaderType.FragmentShader, versionDef + """

				precision highp float;
				out vec4 fragColor;
				void main() { fragColor = vec4(1.0); }
				""");
			_updateProgram = gl.CreateProgram();
			gl.AttachShader(_updateProgram, updateVs);
			gl.AttachShader(_updateProgram, updateFs);
			var varyings = new[] { "vPos" };
			gl.TransformFeedbackVaryings(_updateProgram, (uint)varyings.Length, varyings, TransformFeedbackBufferMode.InterleavedAttribs);
			gl.LinkProgram(_updateProgram);
			gl.GetProgram(_updateProgram, ProgramPropertyARB.LinkStatus, out int linkStatus);
			if (linkStatus != (int)GLEnum.True)
			{
				throw new Exception("Update program link failed: " + gl.GetProgramInfoLog(_updateProgram));
			}
			gl.DetachShader(_updateProgram, updateVs);
			gl.DetachShader(_updateProgram, updateFs);
			gl.DeleteShader(updateVs);
			gl.DeleteShader(updateFs);
			_uTimeLoc = gl.GetUniformLocation(_updateProgram, "uTime");

			// Introspect the registered varying.
			uint varyingNameLength = 0;
			uint varyingSize = 0;
			GLEnum varyingType = 0;
			var varyingNameBuffer = stackalloc byte[256];
			gl.GetTransformFeedbackVarying(_updateProgram, 0, 256, &varyingNameLength, &varyingSize, &varyingType, varyingNameBuffer);
			var varyingName = System.Text.Encoding.UTF8.GetString(varyingNameBuffer, (int)varyingNameLength);
			if (varyingName != "vPos" || varyingSize != 1u || varyingType != GLEnum.FloatVec2)
			{
				throw new Exception($"GetTransformFeedbackVarying returned '{varyingName}' size={varyingSize} type=0x{(int)varyingType:x}");
			}

			// TF object: the TRANSFORM_FEEDBACK_BUFFER binding below is stored inside it.
			_tf = gl.GenTransformFeedback();
			gl.BindTransformFeedback(GLEnum.TransformFeedback, _tf);
			gl.BindBufferRange(BufferTargetARB.TransformFeedbackBuffer, 0, _captureVbo, 0, (nuint)(PointCount * 2 * sizeof(float)));
			if (!gl.IsTransformFeedback(_tf))
			{
				throw new Exception("IsTransformFeedback returned false");
			}
			gl.BindTransformFeedback(GLEnum.TransformFeedback, 0);

			// Render program: draws the captured points.
			_renderProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				out float vR;
				void main() {
				    gl_Position = vec4(aPos, 0.0, 1.0);
				    gl_PointSize = 3.0;
				    vR = length(aPos);
				}
				""",
				versionDef + """

				precision highp float;
				in float vR;
				out vec4 fragColor;
				void main() { fragColor = vec4(0.4 + vR, 0.9 - vR, 1.0, 1.0); }
				""");
		}

		private static unsafe uint MakeVao(GL gl, uint vbo)
		{
			var vao = gl.GenVertexArray();
			gl.BindVertexArray(vao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			gl.BindVertexArray(0);
			return vao;
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_sourceVao);
			gl.DeleteVertexArray(_renderVao);
			gl.DeleteBuffer(_sourceVbo);
			gl.DeleteBuffer(_captureVbo);
			gl.DeleteBuffer(_renderVbo);
			gl.DeleteTransformFeedback(_tf);
			gl.DeleteProgram(_updateProgram);
			gl.DeleteProgram(_renderProgram);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			// --- Update pass: capture orbit positions through the TF object, in two halves ---
			gl.UseProgram(_updateProgram);
			gl.Uniform1(_uTimeLoc, t);
			gl.BindVertexArray(_sourceVao);
			gl.BindTransformFeedback(GLEnum.TransformFeedback, _tf);
			gl.Enable(EnableCap.RasterizerDiscard);
			gl.BeginTransformFeedback(PrimitiveType.Points);
			gl.DrawArrays(PrimitiveType.Points, 0, PointCount / 2);
			gl.PauseTransformFeedback();
			// Decoy while paused: NOT captured. If Pause/Resume were broken this would
			// overwrite the second half with the first points again, collapsing the ring.
			gl.DrawArrays(PrimitiveType.Points, 0, PointCount / 2);
			gl.ResumeTransformFeedback();
			gl.DrawArrays(PrimitiveType.Points, PointCount / 2, PointCount / 2);
			gl.EndTransformFeedback();
			gl.Disable(EnableCap.RasterizerDiscard);
			gl.BindTransformFeedback(GLEnum.TransformFeedback, 0);

			// Server-side wait: the copy below must observe the completed capture.
			var sync = gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, (SyncBehaviorFlags)0);
			gl.WaitSync(sync, (SyncBehaviorFlags)0, unchecked((ulong)-1L) /* GL_TIMEOUT_IGNORED */);
			gl.DeleteSync(sync);

			// --- Copy captured positions into the render buffer ---
			gl.BindBuffer(BufferTargetARB.CopyReadBuffer, _captureVbo);
			gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _renderVbo);
			gl.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.CopyWriteBuffer, 0, 0, (nuint)(PointCount * 2 * sizeof(float)));
			gl.BindBuffer(BufferTargetARB.CopyReadBuffer, 0);
			gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, 0);

			// --- Render pass: ClearBuffer variants + the points ---
			gl.ClearColor(0.04f, 0.05f, 0.09f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			// Depth+stencil in one call (fi), then the stencil alone again (iv).
			gl.ClearBuffer(GLEnum.DepthStencil, 0, 1f, 0);
			int stencilClear = 0;
			gl.ClearBuffer(GLEnum.Stencil, 0, in stencilClear);

			gl.UseProgram(_renderProgram);
			gl.BindVertexArray(_renderVao);
			gl.DrawArrays(PrimitiveType.Points, 0, PointCount);

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
