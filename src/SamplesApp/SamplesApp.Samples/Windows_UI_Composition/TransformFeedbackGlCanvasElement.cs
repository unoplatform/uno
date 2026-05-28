#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests transform feedback (WebGL2's gl_Position-less GPU compute):
	//   - gl.TransformFeedbackVaryings (set varyings before linking)
	//   - gl.BindBufferBase(TRANSFORM_FEEDBACK_BUFFER, ...)
	//   - gl.Enable/Disable(RASTERIZER_DISCARD) during the update pass
	//   - gl.BeginTransformFeedback / gl.EndTransformFeedback
	//   - Ping-pong VBOs (source-VBO vs feedback-VBO swap each frame)
	//
	// A simple 2D particle system: positions and velocities live in a VBO; the update
	// vertex shader writes the next-frame pos/vel via transform feedback into the other VBO;
	// the render pass reads from whichever VBO is current and draws points.
	public class GLCanvasElement_TransformFeedbackElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int ParticleCount = 1024;
		private const int FloatsPerParticle = 4; // vec2 pos + vec2 vel

		private uint _vboA, _vboB;
		private uint _vaoA, _vaoB;
		private uint _updateProgram, _renderProgram;
		private int _uDtLoc;
		private bool _useA = true;
		private DateTime _lastFrame;

		protected override unsafe void Init(GL gl)
		{
			_lastFrame = DateTime.UtcNow;

			// Initial particle data: random positions + small random velocities.
			var data = new float[ParticleCount * FloatsPerParticle];
			var rng = new Random(12345);
			for (int i = 0; i < ParticleCount; i++)
			{
				data[i * 4 + 0] = (float)(rng.NextDouble() * 2 - 1);
				data[i * 4 + 1] = (float)(rng.NextDouble() * 2 - 1);
				data[i * 4 + 2] = (float)((rng.NextDouble() - 0.5) * 0.3);
				data[i * 4 + 3] = (float)((rng.NextDouble() - 0.5) * 0.3);
			}

			_vboA = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboA);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(data), BufferUsageARB.DynamicCopy);

			_vboB = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboB);
			gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(ParticleCount * FloatsPerParticle * sizeof(float)), null, BufferUsageARB.DynamicCopy);

			_vaoA = MakeParticleVao(gl, _vboA);
			_vaoB = MakeParticleVao(gl, _vboB);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			// Update program: feedback-writes the next pos/vel. Must set varyings before linking.
			var updateVs = CompileShader(gl, ShaderType.VertexShader, versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				layout(location = 1) in vec2 aVel;
				uniform float uDt;
				out vec2 vPos;
				out vec2 vVel;
				void main() {
				    vec2 p = aPos + aVel * uDt;
				    // Wrap around edges so particles never escape the viewport.
				    if (p.x < -1.0) p.x += 2.0;
				    else if (p.x > 1.0) p.x -= 2.0;
				    if (p.y < -1.0) p.y += 2.0;
				    else if (p.y > 1.0) p.y -= 2.0;
				    vPos = p;
				    vVel = aVel;
				    // gl_Position must be written even with RASTERIZER_DISCARD enabled;
				    // some implementations leave the TF output buffer as zeros otherwise.
				    gl_Position = vec4(p, 0.0, 1.0);
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
			var varyings = new[] { "vPos", "vVel" };
			gl.TransformFeedbackVaryings(_updateProgram, (uint)varyings.Length, varyings, TransformFeedbackBufferMode.InterleavedAttribs);
			gl.LinkProgram(_updateProgram);
			gl.GetProgram(_updateProgram, ProgramPropertyARB.LinkStatus, out int updateLink);
			if (updateLink != (int)GLEnum.True)
			{
				throw new Exception("Update program link failed: " + gl.GetProgramInfoLog(_updateProgram));
			}
			// Sanity-check that TF varyings were actually registered with the linker. If this is
			// 0 the runtime won't capture anything and the destination buffer stays zero.
			gl.GetProgram(_updateProgram, (ProgramPropertyARB)0x8C83 /* GL_TRANSFORM_FEEDBACK_VARYINGS */, out int tfCount);
			if (tfCount != varyings.Length)
			{
				throw new Exception($"Transform feedback varyings not registered (got {tfCount}, expected {varyings.Length}). Check that transformFeedbackVaryings is called BEFORE linkProgram and that the JS shim is propagating the names.");
			}
			gl.DetachShader(_updateProgram, updateVs);
			gl.DetachShader(_updateProgram, updateFs);
			gl.DeleteShader(updateVs);
			gl.DeleteShader(updateFs);
			_uDtLoc = gl.GetUniformLocation(_updateProgram, "uDt");

			// Render program.
			_renderProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				layout(location = 1) in vec2 aVel;
				out vec3 vColor;
				void main() {
				    gl_Position = vec4(aPos, 0.0, 1.0);
				    gl_PointSize = 3.0;
				    float speed = length(aVel);
				    vColor = vec3(0.5 + speed, 0.8, 1.0 - speed);
				}
				""",
				versionDef + """

				precision highp float;
				in vec3 vColor;
				out vec4 fragColor;
				void main() { fragColor = vec4(vColor, 1.0); }
				""");
		}

		private static unsafe uint MakeParticleVao(GL gl, uint vbo)
		{
			var vao = gl.GenVertexArray();
			gl.BindVertexArray(vao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, FloatsPerParticle * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, FloatsPerParticle * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);
			// Leave ARRAY_BUFTER unbound. VAOs capture the per-attribute buffer references but
			// NOT the generic ARRAY_BUFFER binding, so leaving this set could let the buffer
			// linger as a non-TF binding and trip WebGL2's "TF buffer also bound elsewhere" check.
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			return vao;
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vaoA);
			gl.DeleteVertexArray(_vaoB);
			gl.DeleteBuffer(_vboA);
			gl.DeleteBuffer(_vboB);
			gl.DeleteProgram(_updateProgram);
			gl.DeleteProgram(_renderProgram);
		}

		protected override void RenderOverride(GL gl)
		{
			var now = DateTime.UtcNow;
			var dt = (float)Math.Min(0.05, (now - _lastFrame).TotalSeconds);
			_lastFrame = now;

			var sourceVao = _useA ? _vaoA : _vaoB;
			var destVbo = _useA ? _vboB : _vboA;

			// --- Update pass: read from source VAO, write to dest VBO via TF, no fragment work.
			gl.UseProgram(_updateProgram);
			gl.Uniform1(_uDtLoc, dt);
			gl.BindVertexArray(sourceVao);
			// The generic ARRAY_BUFFER binding is NOT captured by VAOs (only per-attribute
			// bindings + ELEMENT_ARRAY_BUFFER are). Our Init's last BindBuffer(ARRAY_BUFFER, _vboB)
			// in MakeParticleVao is still in effect, and WebGL2 errors with INVALID_OPERATION if a
			// TF output buffer is also bound to any non-TF target. Explicitly clear it.
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			gl.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, destVbo);
			gl.Enable(EnableCap.RasterizerDiscard);
			gl.BeginTransformFeedback(PrimitiveType.Points);
			gl.DrawArrays(PrimitiveType.Points, 0, ParticleCount);
			gl.EndTransformFeedback();
			gl.Disable(EnableCap.RasterizerDiscard);
			gl.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, 0);

			_useA = !_useA;
			var renderVao = _useA ? _vaoA : _vaoB;

			// --- Render pass: draw from the just-updated buffer.
			gl.ClearColor(0.04f, 0.04f, 0.08f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_renderProgram);
			gl.BindVertexArray(renderVao);
			gl.DrawArrays(PrimitiveType.Points, 0, ParticleCount);

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
