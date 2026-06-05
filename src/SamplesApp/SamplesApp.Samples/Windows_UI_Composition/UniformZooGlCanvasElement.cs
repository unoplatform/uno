#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Self-checking stress test for the uniform-setter zoo and the introspection/getter surface.
	// Init throws on any mismatch, so a visible (pulsing green) quad means every check passed.
	//
	//   - every glUniform{1..4}{i,ui}[v] variant, glUniform3f, glUniform{2,4}fv,
	//     and all nine glUniformMatrix*fv shapes
	//   - glGetUniformiv / glGetUniformuiv readback verification
	//   - glBindAttribLocation (pre-link) + glGetAttribLocation + glGetActiveAttrib
	//   - glGetFragDataLocation
	//   - uniform-block introspection: glGetActiveUniformBlockName / glGetActiveUniformBlockiv /
	//     glGetUniformIndices / glGetActiveUniformsiv
	//   - the glIs* family (program, shader, buffer, texture, framebuffer, renderbuffer,
	//     vertex array, sampler, query, sync) + glIsEnabled
	//   - getter zoo: glGetBooleanv, glGetFloatv, glGetInteger64v, glGetShaderSource,
	//     glGetShaderPrecisionFormat, glGetTexParameter{f,i}v, glGetSamplerParameter{f,i}v,
	//     glGetBufferParameter{iv,i64v}, glGetRenderbufferParameteriv,
	//     glGetFramebufferAttachmentParameteriv, glGetInternalformativ, glGetQueryiv
	//   - glTexParameter{f,fv,iv}, glSamplerParameter{f,fv,iv}, glHint, glFinish
	public class GLCanvasElement_UniformZooElement() : GLCanvasElement(() => App.MainWindow)
	{
		private uint _vao, _vbo, _ubo, _program;
		private int _uTimeLoc;
		private DateTime _startTime;

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			var vsSource = versionDef + """

				precision highp float;
				in vec2 aPos;
				void main() { gl_Position = vec4(aPos, 0.0, 1.0); }
				""";
			var fsSource = versionDef + """

				precision highp float;
				precision highp int;
				uniform float uTime;
				uniform vec3 f3;
				uniform vec2 f2a[2];
				uniform vec4 f4a[2];
				uniform ivec2 i2;
				uniform ivec3 i3;
				uniform ivec4 i4;
				uniform int ia[2];
				uniform ivec2 i2a[2];
				uniform ivec3 i3a[2];
				uniform ivec4 i4a[2];
				uniform uint u1;
				uniform uvec2 u2;
				uniform uvec3 u3;
				uniform uvec4 u4;
				uniform uint ua[2];
				uniform uvec2 u2a[2];
				uniform uvec3 u3a[2];
				uniform uvec4 u4a[2];
				uniform mat2 m2;
				uniform mat3 m3;
				uniform mat2x3 m23;
				uniform mat2x4 m24;
				uniform mat3x2 m32;
				uniform mat3x4 m34;
				uniform mat4x2 m42;
				uniform mat4x3 m43;
				layout(std140) uniform ZooBlock { vec4 zb; };
				out vec4 fragColor;
				void main() {
				    // Reference every uniform so all of them stay active; scale the sum down so the
				    // quad stays visibly green when the values are as expected.
				    float sum = f3.x + f2a[1].y + f4a[0].z
				        + float(i2.x + i3.y + i4.z + ia[0] + i2a[1].x + i3a[0].z + i4a[1].w)
				        + float(u1 + u2.y + u3.z + u4.w + ua[1] + u2a[0].x + u3a[1].y + u4a[0].z)
				        + m2[0][0] + m3[1][1] + m23[1][2] + m24[0][3] + m32[2][1] + m34[2][3]
				        + m42[3][1] + m43[3][2] + zb.x;
				    float pulse = 0.75 + 0.25 * sin(uTime * 2.0);
				    fragColor = vec4(0.1, 0.8, 0.3, 1.0) * pulse + vec4(sum * 0.0001);
				}
				""";

			var vs = CompileShader(gl, ShaderType.VertexShader, vsSource);
			var fs = CompileShader(gl, ShaderType.FragmentShader, fsSource);
			_program = gl.CreateProgram();
			gl.AttachShader(_program, vs);
			gl.AttachShader(_program, fs);
			// Pin aPos to location 2 before linking and verify it took effect.
			gl.BindAttribLocation(_program, 2, "aPos");
			gl.LinkProgram(_program);
			gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int linkStatus);
			if (linkStatus != (int)GLEnum.True)
			{
				throw new Exception("Program link failed: " + gl.GetProgramInfoLog(_program));
			}
			Check(gl.GetAttribLocation(_program, "aPos") == 2, "BindAttribLocation was not honored");
			Check(gl.GetFragDataLocation(_program, "fragColor") >= 0, "GetFragDataLocation(fragColor) < 0");

			// glGetShaderSource round-trip while the shader is still alive.
			gl.GetShaderSource(fs, 16 * 1024, out _, out string fetchedSource);
			Check(fetchedSource.Contains("ZooBlock", StringComparison.Ordinal), "GetShaderSource did not round-trip");
			gl.GetShaderPrecisionFormat(ShaderType.FragmentShader, GLEnum.HighFloat, out int range, out int precision);
			Check(precision > 0, "GetShaderPrecisionFormat reported zero precision for highp float");
			Check(gl.IsShader(fs), "IsShader(fs) was false");

			gl.DetachShader(_program, vs);
			gl.DetachShader(_program, fs);
			gl.DeleteShader(vs);
			gl.DeleteShader(fs);
			Check(gl.IsProgram(_program), "IsProgram was false");

			// glGetActiveAttrib: aPos must be reported.
			gl.GetProgram(_program, ProgramPropertyARB.ActiveAttributes, out int attribCount);
			var foundPos = false;
			for (uint i = 0; i < attribCount; i++)
			{
				var name = gl.GetActiveAttrib(_program, i, out int attribSize, out AttributeType attribType);
				foundPos |= name == "aPos" && attribSize == 1 && attribType == AttributeType.FloatVec2;
			}
			Check(foundPos, "GetActiveAttrib did not report aPos");

			// --- Set every uniform variant and spot-check the i/ui readbacks ---
			gl.UseProgram(_program);
			int Loc(string name) => gl.GetUniformLocation(_program, name);

			gl.Uniform3(Loc("f3"), 1f, 2f, 3f);
			gl.Uniform2(Loc("f2a[0]"), 2, new ReadOnlySpan<float>(new float[] { 1, 2, 3, 4 }));
			gl.Uniform4(Loc("f4a[0]"), 2, new ReadOnlySpan<float>(new float[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

			gl.Uniform2(Loc("i2"), 1, 2);
			gl.Uniform3(Loc("i3"), 1, 2, 3);
			gl.Uniform4(Loc("i4"), 1, 2, 3, 4);
			gl.Uniform1(Loc("ia[0]"), 2, new ReadOnlySpan<int>(new int[] { 5, 6 }));
			gl.Uniform2(Loc("i2a[0]"), 2, new ReadOnlySpan<int>(new int[] { 1, 2, 3, 4 }));
			gl.Uniform3(Loc("i3a[0]"), 2, new ReadOnlySpan<int>(new int[] { 1, 2, 3, 4, 5, 6 }));
			gl.Uniform4(Loc("i4a[0]"), 2, new ReadOnlySpan<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

			gl.Uniform1(Loc("u1"), 7u);
			gl.Uniform2(Loc("u2"), 1u, 2u);
			gl.Uniform3(Loc("u3"), 1u, 2u, 3u);
			gl.Uniform4(Loc("u4"), 1u, 2u, 3u, 4u);
			gl.Uniform1(Loc("ua[0]"), 2, new ReadOnlySpan<uint>(new uint[] { 8, 9 }));
			gl.Uniform2(Loc("u2a[0]"), 2, new ReadOnlySpan<uint>(new uint[] { 1, 2, 3, 4 }));
			gl.Uniform3(Loc("u3a[0]"), 2, new ReadOnlySpan<uint>(new uint[] { 1, 2, 3, 4, 5, 6 }));
			gl.Uniform4(Loc("u4a[0]"), 2, new ReadOnlySpan<uint>(new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

			gl.UniformMatrix2(Loc("m2"), 1, false, new ReadOnlySpan<float>(new float[4] { 1, 0, 0, 1 }));
			gl.UniformMatrix3(Loc("m3"), 1, false, new ReadOnlySpan<float>(new float[9] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }));
			gl.UniformMatrix2x3(Loc("m23"), 1, false, new ReadOnlySpan<float>(new float[6] { 1, 2, 3, 4, 5, 6 }));
			gl.UniformMatrix2x4(Loc("m24"), 1, false, new ReadOnlySpan<float>(new float[8] { 1, 2, 3, 4, 5, 6, 7, 8 }));
			gl.UniformMatrix3x2(Loc("m32"), 1, false, new ReadOnlySpan<float>(new float[6] { 1, 2, 3, 4, 5, 6 }));
			gl.UniformMatrix3x4(Loc("m34"), 1, false, new ReadOnlySpan<float>(new float[12] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }));
			gl.UniformMatrix4x2(Loc("m42"), 1, false, new ReadOnlySpan<float>(new float[8] { 1, 2, 3, 4, 5, 6, 7, 8 }));
			gl.UniformMatrix4x3(Loc("m43"), 1, false, new ReadOnlySpan<float>(new float[12] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }));

			gl.GetUniform(_program, Loc("i4"), out int i4First);
			Check(i4First == 1, $"GetUniformiv(i4) returned {i4First}, expected 1");
			gl.GetUniform(_program, Loc("u4"), out uint u4First);
			Check(u4First == 1u, $"GetUniformuiv(u4) returned {u4First}, expected 1");
			gl.GetUniform(_program, Loc("f3"), out float f3First);
			Check(Math.Abs(f3First - 1f) < 0.001f, $"GetUniformfv(f3) returned {f3First}, expected 1");

			// --- Uniform block introspection + backing UBO ---
			var blockIndex = gl.GetUniformBlockIndex(_program, "ZooBlock");
			Check(blockIndex != unchecked((uint)-1), "GetUniformBlockIndex(ZooBlock) failed");
			gl.GetActiveUniformBlockName(_program, blockIndex, 256, out _, out string blockName);
			Check(blockName == "ZooBlock", $"GetActiveUniformBlockName returned '{blockName}'");
			gl.GetActiveUniformBlock(_program, blockIndex, UniformBlockPName.UniformBlockDataSize, out int blockSize);
			Check(blockSize >= 16, $"GetActiveUniformBlockiv(DataSize) returned {blockSize}");

			var indices = new uint[1];
			gl.GetUniformIndices(_program, 1, new string[] { "zb" }, out indices[0]);
			Check(indices[0] != unchecked((uint)-1), "GetUniformIndices(zb) failed");
			var types = new int[1];
			gl.GetActiveUniforms(_program, 1, indices, UniformPName.UniformType, types);
			Check(types[0] == (int)GLEnum.FloatVec4, $"GetActiveUniformsiv(zb) returned type 0x{types[0]:x}");

			gl.UniformBlockBinding(_program, blockIndex, 0);
			_ubo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.UniformBuffer, _ubo);
			var zb = new float[] { 0.5f, 0, 0, 0 };
			gl.BufferData(BufferTargetARB.UniformBuffer, new ReadOnlySpan<float>(zb), BufferUsageARB.StaticDraw);
			gl.BindBufferBase(BufferTargetARB.UniformBuffer, 0, _ubo);

			// --- Geometry ---
			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			var quad = new float[] { -1f, -1f, 1f, -1f, 1f, 1f, -1f, -1f, 1f, 1f, -1f, 1f };
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(quad), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(2, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(2);

			// --- The glIs* family and the remaining getters, on small throwaway objects ---
			Check(gl.IsBuffer(_vbo), "IsBuffer(vbo) was false");
			Check(gl.IsVertexArray(_vao), "IsVertexArray was false");
			Check(!gl.IsEnabled(EnableCap.DepthTest), "IsEnabled(DepthTest) was unexpectedly true");

			var tex = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, tex);
			gl.TexStorage2D(TextureTarget.Texture2D, 1, SizedInternalFormat.Rgba8, 2, 2);
			gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxLod, 5f);
			gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMinLod, new ReadOnlySpan<float>(new float[] { -2f }));
			gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, new ReadOnlySpan<int>(new int[] { (int)GLEnum.ClampToEdge }));
			Check(gl.IsTexture(tex), "IsTexture was false");
			gl.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureWrapS, out int wrapS);
			Check(wrapS == (int)GLEnum.ClampToEdge, $"GetTexParameteriv(WrapS) returned 0x{wrapS:x}");
			gl.GetTexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxLod, out float maxLod);
			Check(Math.Abs(maxLod - 5f) < 0.001f, $"GetTexParameterfv(MaxLod) returned {maxLod}");

			var sampler = gl.GenSampler();
			gl.SamplerParameter(sampler, SamplerParameterF.TextureMaxLod, 3f);
			gl.SamplerParameter(sampler, SamplerParameterF.TextureMinLod, new ReadOnlySpan<float>(new float[] { -1f }));
			gl.SamplerParameter(sampler, SamplerParameterI.WrapT, new ReadOnlySpan<int>(new int[] { (int)GLEnum.Repeat }));
			Check(gl.IsSampler(sampler), "IsSampler was false");
			gl.GetSamplerParameter(sampler, SamplerParameterF.TextureMaxLod, out float samplerMaxLod);
			Check(Math.Abs(samplerMaxLod - 3f) < 0.001f, $"GetSamplerParameterfv returned {samplerMaxLod}");
			gl.GetSamplerParameter(sampler, SamplerParameterI.WrapT, out int samplerWrapT);
			Check(samplerWrapT == (int)GLEnum.Repeat, $"GetSamplerParameteriv returned 0x{samplerWrapT:x}");
			gl.DeleteSampler(sampler);

			var rbo = gl.GenRenderbuffer();
			gl.BindRenderbuffer(GLEnum.Renderbuffer, rbo);
			gl.RenderbufferStorage(GLEnum.Renderbuffer, InternalFormat.Rgba8, 4, 4);
			Check(gl.IsRenderbuffer(rbo), "IsRenderbuffer was false");
			gl.GetRenderbufferParameter(GLEnum.Renderbuffer, GLEnum.RenderbufferWidth, out int rboWidth);
			Check(rboWidth == 4, $"GetRenderbufferParameteriv(Width) returned {rboWidth}");
			gl.GetInternalformat(GLEnum.Renderbuffer, GLEnum.Rgba8, GLEnum.Samples, 1, out int internalformatSamples);
			Check(internalformatSamples >= 0, "GetInternalformativ failed");

			var fbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, fbo);
			gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Renderbuffer, rbo);
			Check(gl.IsFramebuffer(fbo), "IsFramebuffer was false");
			gl.GetFramebufferAttachmentParameter(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.FramebufferAttachmentObjectType, out int attachmentType);
			Check(attachmentType == (int)GLEnum.Renderbuffer, $"GetFramebufferAttachmentParameteriv returned 0x{attachmentType:x}");
			gl.BindFramebuffer(GLEnum.Framebuffer, 0);
			gl.DeleteFramebuffer(fbo);
			gl.DeleteRenderbuffer(rbo);
			gl.DeleteTexture(tex);

			var query = gl.GenQuery();
			gl.BeginQuery(QueryTarget.AnySamplesPassed, query);
			gl.GetQuery(QueryTarget.AnySamplesPassed, GLEnum.CurrentQuery, out int currentQuery);
			Check(currentQuery == (int)query, $"GetQueryiv(CurrentQuery) returned {currentQuery}");
			gl.EndQuery(QueryTarget.AnySamplesPassed);
			Check(gl.IsQuery(query), "IsQuery was false");
			gl.DeleteQuery(query);

			var sync = gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, (SyncBehaviorFlags)0);
			Check(gl.IsSync(sync), "IsSync was false");
			gl.DeleteSync(sync);

			gl.GetBoolean(GLEnum.DepthWritemask, out bool depthMask);
			Check(depthMask, "GetBooleanv(DepthWritemask) returned false");
			gl.GetFloat(GLEnum.LineWidth, out float lineWidth);
			Check(Math.Abs(lineWidth - 1f) < 0.001f, $"GetFloatv(LineWidth) returned {lineWidth}");
			gl.GetInteger64(GLEnum.MaxElementIndex, out long maxElementIndex);
			Check(maxElementIndex > 0, $"GetInteger64v(MaxElementIndex) returned {maxElementIndex}");
			gl.GetBufferParameter(BufferTargetARB.ArrayBuffer, GLEnum.BufferSize, out int bufferSize);
			Check(bufferSize == quad.Length * sizeof(float), $"GetBufferParameteriv(Size) returned {bufferSize}");
			gl.GetBufferParameter(BufferTargetARB.ArrayBuffer, GLEnum.BufferSize, out long bufferSize64);
			Check(bufferSize64 == quad.Length * sizeof(float), $"GetBufferParameteri64v(Size) returned {bufferSize64}");

			gl.Hint(HintTarget.FragmentShaderDerivativeHint, HintMode.Nicest);
			gl.Finish();

			_uTimeLoc = Loc("uTime");

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
				throw new Exception("UniformZoo check failed: " + message);
			}
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteBuffer(_ubo);
			gl.DeleteProgram(_program);
		}

		protected override void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			gl.ClearColor(0.05f, 0.05f, 0.07f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);

			gl.UseProgram(_program);
			gl.Uniform1(_uTimeLoc, t);
			gl.BindVertexArray(_vao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

			Invalidate();
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
