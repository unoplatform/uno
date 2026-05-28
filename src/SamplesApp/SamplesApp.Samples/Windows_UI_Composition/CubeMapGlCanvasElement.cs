#if __SKIA__ || WINAPPSDK
using System;
using System.Numerics;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests cubemap textures:
	//   - TEXTURE_CUBE_MAP target with six gl.TexImage2D uploads (one per face)
	//   - gl.TexParameteri for cubemap-specific filter/wrap modes
	//   - samplerCube binding + sampling in the fragment shader
	//   - Continuous yaw rotation to verify each face is reached
	//
	// Renders the cubemap as a skybox-style background (inside-facing cube) using a small
	// vertex buffer of cube corners. Each face is a single solid color so it's easy to see
	// which face is which when rotating.
	public class GLCanvasElement_CubeMapElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int FaceSize = 32;

		private uint _vao, _vbo;
		private uint _cubemap;
		private uint _program;
		private int _uViewLoc, _uProjLoc, _uCubeLoc;
		private DateTime _startTime;

		// 36 vertices, two triangles per cube face. Position only (xyz). Each face is rendered
		// from the inside so winding is reversed.
		private static readonly float[] _cubeVertices =
		{
			// +X face
			 1, -1, -1,  1, -1,  1,  1,  1,  1,
			 1, -1, -1,  1,  1,  1,  1,  1, -1,
			// -X face
			-1, -1,  1, -1, -1, -1, -1,  1, -1,
			-1, -1,  1, -1,  1, -1, -1,  1,  1,
			// +Y face
			-1,  1, -1,  1,  1, -1,  1,  1,  1,
			-1,  1, -1,  1,  1,  1, -1,  1,  1,
			// -Y face
			-1, -1,  1,  1, -1,  1,  1, -1, -1,
			-1, -1,  1,  1, -1, -1, -1, -1, -1,
			// +Z face
			-1, -1,  1, -1,  1,  1,  1,  1,  1,
			-1, -1,  1,  1,  1,  1,  1, -1,  1,
			// -Z face
			 1, -1, -1,  1,  1, -1, -1,  1, -1,
			 1, -1, -1, -1,  1, -1, -1, -1, -1,
		};

		// Six face colors so it's visually obvious which face is which.
		private static readonly (byte r, byte g, byte b)[] _faceColors =
		{
			(220,  60,  60), // +X red
			( 60, 220,  60), // -X green
			( 60,  60, 220), // +Y blue
			(220, 220,  60), // -Y yellow
			(220,  60, 220), // +Z magenta
			( 60, 220, 220), // -Z cyan
		};

		private static readonly GLEnum[] _faceTargets =
		{
			GLEnum.TextureCubeMapPositiveX,
			GLEnum.TextureCubeMapNegativeX,
			GLEnum.TextureCubeMapPositiveY,
			GLEnum.TextureCubeMapNegativeY,
			GLEnum.TextureCubeMapPositiveZ,
			GLEnum.TextureCubeMapNegativeZ,
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_cubeVertices), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 3 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			// Upload the six cubemap faces.
			_cubemap = gl.GenTexture();
			gl.BindTexture(TextureTarget.TextureCubeMap, _cubemap);
			var pixels = new byte[FaceSize * FaceSize * 4];
			for (int face = 0; face < 6; face++)
			{
				var (r, g, b) = _faceColors[face];
				for (int i = 0; i < FaceSize * FaceSize; i++)
				{
					pixels[i * 4 + 0] = r;
					pixels[i * 4 + 1] = g;
					pixels[i * 4 + 2] = b;
					pixels[i * 4 + 3] = 255;
				}
				fixed (byte* p = pixels)
				{
					gl.TexImage2D(_faceTargets[face], 0, InternalFormat.Rgba, FaceSize, FaceSize, 0, GLEnum.Rgba, GLEnum.UnsignedByte, p);
				}
			}
			gl.TexParameterI(TextureTarget.TextureCubeMap, GLEnum.TextureMinFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.TextureCubeMap, GLEnum.TextureMagFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.TextureCubeMap, GLEnum.TextureWrapS, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(TextureTarget.TextureCubeMap, GLEnum.TextureWrapT, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(TextureTarget.TextureCubeMap, GLEnum.TextureWrapR, (uint)GLEnum.ClampToEdge);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			_program = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec3 aPos;
				uniform mat4 uView;
				uniform mat4 uProj;
				out vec3 vDir;
				void main() {
				    vDir = aPos;
				    gl_Position = uProj * uView * vec4(aPos, 1.0);
				}
				""",
				versionDef + """

				precision highp float;
				in vec3 vDir;
				out vec4 fragColor;
				uniform samplerCube uCube;
				void main() { fragColor = texture(uCube, normalize(vDir)); }
				""");
			_uViewLoc = gl.GetUniformLocation(_program, "uView");
			_uProjLoc = gl.GetUniformLocation(_program, "uProj");
			_uCubeLoc = gl.GetUniformLocation(_program, "uCube");
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteTexture(_cubemap);
			gl.DeleteProgram(_program);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			gl.Enable(EnableCap.DepthTest);
			gl.ClearColor(0f, 0f, 0f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// Camera rotates around Y; viewing the cube from inside (we're at the origin, cube at +/-1).
			var view = Matrix4x4.CreateRotationY(t * 0.4f) * Matrix4x4.CreateRotationX(MathF.Sin(t * 0.3f) * 0.25f);
			var proj = SimplePerspective(0.6f);

			gl.UseProgram(_program);
			gl.UniformMatrix4(_uViewLoc, 1, false, (float*)&view);
			gl.UniformMatrix4(_uProjLoc, 1, false, (float*)&proj);

			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.TextureCubeMap, _cubemap);
			gl.Uniform1(_uCubeLoc, 0);

			gl.BindVertexArray(_vao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

			gl.Disable(EnableCap.DepthTest);
			Invalidate();
		}

		private static Matrix4x4 SimplePerspective(float scale)
		{
			const float n = 0.5f, f = 8f;
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
