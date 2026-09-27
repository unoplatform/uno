#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests asynchronous queries, fence syncs, uniform arrays, and program introspection:
	//   - gl.GenQuery / gl.BeginQuery(ANY_SAMPLES_PASSED) / gl.EndQuery
	//   - gl.GetQueryObject (QUERY_RESULT_AVAILABLE polled across frames, then QUERY_RESULT)
	//   - gl.FenceSync / gl.ClientWaitSync (0 timeout, per WebGL2) / gl.GetSync / gl.DeleteSync
	//   - array uniforms via gl.Uniform1 (count overload, float[8]) and gl.Uniform3 (vec3[4])
	//   - gl.GetUniform readback sanity check
	//   - gl.GetProgram(ACTIVE_UNIFORMS) + gl.GetActiveUniform + gl.ValidateProgram (Init-time)
	//   - gl.Flush
	//
	// Renders 8 waving bars colored from a palette array. A small "probe" quad is drawn behind
	// one of the center bars inside an occlusion query; the indicator square in the top-left
	// corner is red while the probe is fully occluded by the bar and green while any of its
	// samples pass, so it alternates with the bar's wave.
	public class GLCanvasElement_QuerySyncElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int BarCount = 8;

		private uint _barsVao, _quadVbo, _barsProgram;
		private int _uWaveLoc, _uPaletteLoc;
		private uint _probeVao, _probeVbo, _probeProgram;
		private int _probeUColorLoc, _probeURectLoc;

		private uint _query;
		private bool _queryInFlight;
		private bool _lastProbeVisible;
		private nint _frameSync;
		private DateTime _startTime;

		private static readonly float[] _unitQuad =
		{
			0f, 0f,
			1f, 0f,
			1f, 1f,
			0f, 0f,
			1f, 1f,
			0f, 1f,
		};

		private static readonly float[] _palette =
		{
			0.9f, 0.3f, 0.3f,
			0.3f, 0.9f, 0.4f,
			0.3f, 0.5f, 0.9f,
			0.9f, 0.8f, 0.3f,
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			// Bars: a unit quad instanced over gl_InstanceID; height from uWave[i], color from uPalette.
			_barsProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				uniform float uWave[8];
				uniform vec3 uPalette[4];
				out vec3 vColor;
				void main() {
				    float i = float(gl_InstanceID);
				    float width = 2.0 / 8.0;
				    float x = -1.0 + i * width + aPos.x * width * 0.8 + width * 0.1;
				    float y = -0.9 + aPos.y * (0.2 + uWave[gl_InstanceID] * 1.5);
				    gl_Position = vec4(x, y, 0.5, 1.0);
				    vColor = uPalette[gl_InstanceID - (gl_InstanceID / 4) * 4];
				}
				""",
				versionDef + """

				precision highp float;
				in vec3 vColor;
				out vec4 fragColor;
				void main() { fragColor = vec4(vColor, 1.0); }
				""");
			_uWaveLoc = gl.GetUniformLocation(_barsProgram, "uWave");
			_uPaletteLoc = gl.GetUniformLocation(_barsProgram, "uPalette");

			// Probe/indicator: a solid-color quad placed via a rect uniform (x, y, w, h in NDC).
			_probeProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				uniform vec4 uRect;
				void main() {
				    gl_Position = vec4(uRect.xy + aPos * uRect.zw, 0.9, 1.0);
				}
				""",
				versionDef + """

				precision highp float;
				uniform vec4 uColor;
				out vec4 fragColor;
				void main() { fragColor = uColor; }
				""");
			_probeURectLoc = gl.GetUniformLocation(_probeProgram, "uRect");
			_probeUColorLoc = gl.GetUniformLocation(_probeProgram, "uColor");

			_quadVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_unitQuad), BufferUsageARB.StaticDraw);

			_barsVao = gl.GenVertexArray();
			gl.BindVertexArray(_barsVao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			_probeVao = gl.GenVertexArray();
			gl.BindVertexArray(_probeVao);
			_probeVbo = _quadVbo;
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _probeVbo);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			// The palette never changes; upload once and read it back as a sanity check.
			gl.UseProgram(_barsProgram);
			gl.Uniform3(_uPaletteLoc, 4, new ReadOnlySpan<float>(_palette));
			gl.GetUniform(_barsProgram, _uPaletteLoc, out float firstComponent);
			if (Math.Abs(firstComponent - _palette[0]) > 0.001f)
			{
				throw new Exception($"GetUniform readback mismatch: got {firstComponent}, expected {_palette[0]}");
			}

			// Program introspection: enumerate active uniforms and validate the program.
			gl.GetProgram(_barsProgram, ProgramPropertyARB.ActiveUniforms, out int activeUniforms);
			var foundWave = false;
			for (uint i = 0; i < activeUniforms; i++)
			{
				var name = gl.GetActiveUniform(_barsProgram, i, out int size, out UniformType type);
				// Array uniforms report as "uWave[0]" with size = element count.
				foundWave |= name.StartsWith("uWave", StringComparison.Ordinal) && size == BarCount && type == UniformType.Float;
			}
			if (!foundWave)
			{
				throw new Exception($"GetActiveUniform did not report uWave[{BarCount}] (saw {activeUniforms} active uniforms)");
			}
			gl.ValidateProgram(_barsProgram);
			gl.GetProgram(_barsProgram, ProgramPropertyARB.ValidateStatus, out int validateStatus);
			if (validateStatus != (int)GLEnum.True)
			{
				throw new Exception("ValidateProgram failed: " + gl.GetProgramInfoLog(_barsProgram));
			}

			_query = gl.GenQuery();

			// Init must finish error-clean; surface anything the calls above raised.
			var err = gl.GetError();
			if (err != GLEnum.NoError)
			{
				throw new Exception($"GL error at end of Init: 0x{(int)err:x}");
			}
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_barsVao);
			gl.DeleteVertexArray(_probeVao);
			gl.DeleteBuffer(_quadVbo);
			gl.DeleteProgram(_barsProgram);
			gl.DeleteProgram(_probeProgram);
			gl.DeleteQuery(_query);
			if (_frameSync != 0)
			{
				gl.DeleteSync(_frameSync);
				_frameSync = 0;
			}
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			// --- Check last frame's fence; the GPU should long be done by the next paint ---
			if (_frameSync != 0)
			{
				// WebGL2 requires a 0 timeout for ClientWaitSync.
				var waitResult = (GLEnum)gl.ClientWaitSync(_frameSync, (SyncObjectMask)0, 0);
				gl.GetSync(_frameSync, SyncParameterName.SyncStatus, 1, out _, out int syncStatus);
				if (waitResult == GLEnum.WaitFailed)
				{
					throw new Exception($"ClientWaitSync failed (status=0x{syncStatus:x})");
				}
				gl.DeleteSync(_frameSync);
				_frameSync = 0;
			}

			// --- Poll the occlusion query from a previous frame without stalling ---
			if (_queryInFlight)
			{
				gl.GetQueryObject(_query, QueryObjectParameterName.ResultAvailable, out uint available);
				if (available != 0)
				{
					gl.GetQueryObject(_query, QueryObjectParameterName.Result, out uint anySamplesPassed);
					_lastProbeVisible = anySamplesPassed != 0;
					_queryInFlight = false;
				}
			}

			gl.ClearColor(0.06f, 0.06f, 0.08f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			gl.Enable(EnableCap.DepthTest);

			// --- Bars (depth 0.5, drawn first) ---
			gl.UseProgram(_barsProgram);
			var wave = stackalloc float[BarCount];
			for (int i = 0; i < BarCount; i++)
			{
				wave[i] = 0.5f + 0.5f * MathF.Sin(t * 2f + i * 0.7f);
			}
			gl.Uniform1(_uWaveLoc, BarCount, wave);
			gl.BindVertexArray(_barsVao);
			gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, BarCount);

			// --- Probe quad behind bar 4 (depth 0.9), wrapped in an occlusion query. The probe's
			// x-range sits entirely within the bar's footprint (bar 4 spans x = 0.025..0.225, with
			// no gap overlap), so it is fully occluded whenever that bar's wave makes it tall
			// enough (top >= 0.5) and visible above it otherwise - the indicator alternates.
			if (!_queryInFlight)
			{
				gl.UseProgram(_probeProgram);
				gl.Uniform4(_probeURectLoc, 0.05f, 0.2f, 0.15f, 0.3f);
				gl.Uniform4(_probeUColorLoc, 0.5f, 0.5f, 0.5f, 1f);
				gl.BindVertexArray(_probeVao);
				gl.BeginQuery(QueryTarget.AnySamplesPassed, _query);
				gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
				gl.EndQuery(QueryTarget.AnySamplesPassed);
				_queryInFlight = true;
			}

			// --- Indicator square top-left: green = probe visible, red = fully occluded ---
			gl.Disable(EnableCap.DepthTest);
			gl.UseProgram(_probeProgram);
			gl.Uniform4(_probeURectLoc, -0.95f, 0.8f, 0.12f, 0.12f);
			if (_lastProbeVisible)
			{
				gl.Uniform4(_probeUColorLoc, 0.2f, 0.9f, 0.3f, 1f);
			}
			else
			{
				gl.Uniform4(_probeUColorLoc, 0.9f, 0.2f, 0.2f, 1f);
			}
			gl.BindVertexArray(_probeVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

			// --- Fence this frame's commands and kick the GPU ---
			_frameSync = gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, (SyncBehaviorFlags)0);
			gl.Flush();

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
