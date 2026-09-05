#if __SKIA__ || WINAPPSDK
#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Port of the internal WASM-AOT GL repro (a minimal 3D model viewer): a lit sphere
	// encircled by a tilted ring, drawn through GLCanvasElement with a per-vertex normal/UV mesh,
	// a Blinn-Phong shader, and a 1x1 white fallback texture. The file-loading (Assimp) path of the
	// original is dropped - it isn't needed to exercise the reported crash.
	//
	// Why it matters: this hits two 9-arg gl.TexImage2D call sites - the framework's offscreen FBO
	// color-attachment allocation (GLCanvasElement.FrameBufferDetails) and the white-texture upload
	// here. Under WebAssembly full AOT those 9-arg calls dispatch through the mono interpreter's
	// interp->native trampoline, which caps at 8 args (dotnet/runtime#109338), aborting with
	// "RuntimeError: null function" at wasm_invoke_viiiiiiiii. It renders correctly on Desktop and
	// on the WASM interpreter; the crash reproduces only on a Release WASM AOT publish.
	public class GLCanvasElement_ModelViewerElement() : GLCanvasElement(() => App.MainWindow!)
	{
		private Shader? _shader;
		private Model? _model;
		private Texture? _whiteTexture;
		private readonly Camera _camera = new();
		private DateTime _startTime;

		protected override void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			gl.Enable(EnableCap.DepthTest);
			gl.Enable(EnableCap.CullFace);
			gl.CullFace(CullFaceMode.Back);
			gl.FrontFace(FrontFaceDirection.Ccw);

			var sl = gl.GetStringS(StringName.ShadingLanguageVersion);
			var ver = sl.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";

			var vs = $$"""
				{{ver}}
				precision highp float;
				layout(location = 0) in vec3 aPos;
				layout(location = 1) in vec3 aNormal;
				layout(location = 2) in vec2 aUV;
				uniform mat4 uView;
				uniform mat4 uProj;
				out vec3 vNormalV;
				out vec3 vPosV;
				out vec2 vUV;
				void main()
				{
				    vec4 posV = uView * vec4(aPos, 1.0);
				    vPosV = posV.xyz;
				    vNormalV = mat3(uView) * aNormal;
				    vUV = aUV;
				    gl_Position = uProj * posV;
				}
				""";

			// View-space lighting: ambient + a full-range Lambert key light, a softer fill from the
			// opposite side, a Blinn-Phong specular highlight, and a Fresnel rim on silhouettes.
			var fs = $$"""
				{{ver}}
				precision highp float;
				in vec3 vNormalV;
				in vec3 vPosV;
				in vec2 vUV;
				out vec4 outColor;
				uniform mat4 uView;
				uniform vec3 uLightDir;
				uniform vec3 uBaseColor;
				uniform sampler2D uDiffuse;
				void main()
				{
				    vec3 N = normalize(vNormalV);
				    vec3 V = normalize(-vPosV);
				    vec3 L = normalize(mat3(uView) * (-uLightDir));
				    vec3 F = normalize(mat3(uView) * vec3(0.5, 0.35, 0.6));

				    vec3 albedo = pow(uBaseColor, vec3(2.2)) * texture(uDiffuse, vUV).rgb;

				    float key = max(dot(N, L), 0.0);
				    float fill = max(dot(N, F), 0.0);
				    float ambient = 0.12;

				    vec3 H = normalize(L + V);
				    float spec = pow(max(dot(N, H), 0.0), 48.0) * 0.4;

				    float rim = pow(1.0 - max(dot(N, V), 0.0), 3.0) * 0.25;

				    vec3 col = albedo * (ambient + 1.0 * key + 0.28 * fill)
				             + vec3(1.0) * spec
				             + albedo * rim;

				    col = pow(col, vec3(1.0 / 2.2));
				    outColor = vec4(clamp(col, 0.0, 1.0), 1.0);
				}
				""";

			_shader = new Shader(gl, vs, fs);
			_shader.Use();
			_shader.SetInt("uDiffuse", 0);

			_whiteTexture = Texture.CreateWhite(gl);

			_model = Model.CreateShowpiece(gl);
			_camera.FitToBounds(_model.BoundsMin, _model.BoundsMax);
		}

		protected override void OnDestroy(GL gl)
		{
			_shader?.Dispose();
			_model?.Dispose();
			_whiteTexture?.Dispose();
			_shader = null;
			_model = null;
			_whiteTexture = null;
		}

		protected override void RenderOverride(GL gl)
		{
			gl.ClearColor(0.08f, 0.10f, 0.13f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			if (_shader is null || _model is null || _whiteTexture is null)
			{
				return;
			}

			// Gentle continuous orbit so the sample is visibly live.
			_camera.Azimuth = MathF.PI * 0.25f + (float)(DateTime.UtcNow - _startTime).TotalSeconds * 0.5f;

			var aspect = (float)(ActualWidth / Math.Max(1.0, ActualHeight));
			_shader.Use();
			_shader.SetMatrix("uView", _camera.GetViewMatrix());
			_shader.SetMatrix("uProj", _camera.GetProjectionMatrix(aspect));
			_shader.SetVec3("uLightDir", Vector3.Normalize(new Vector3(-0.4f, -1f, -0.3f)));

			foreach (var mesh in _model.Meshes)
			{
				_shader.SetVec3("uBaseColor", mesh.DiffuseColor);
				(mesh.DiffuseTexture ?? _whiteTexture).Bind(0);
				mesh.Draw();
			}

			Invalidate();
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Vertex(Vector3 position, Vector3 normal, Vector2 texCoord)
		{
			public Vector3 Position = position;
			public Vector3 Normal = normal;
			public Vector2 TexCoord = texCoord;
		}

		private sealed class Shader : IDisposable
		{
			private readonly GL _gl;
			private readonly uint _program;

			public Shader(GL gl, string vertexSrc, string fragmentSrc)
			{
				_gl = gl;
				var vs = Compile(ShaderType.VertexShader, vertexSrc);
				var fs = Compile(ShaderType.FragmentShader, fragmentSrc);

				_program = gl.CreateProgram();
				gl.AttachShader(_program, vs);
				gl.AttachShader(_program, fs);
				gl.LinkProgram(_program);
				gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int status);
				if (status != (int)GLEnum.True)
				{
					throw new InvalidOperationException("Program link failed: " + gl.GetProgramInfoLog(_program));
				}

				gl.DetachShader(_program, vs);
				gl.DetachShader(_program, fs);
				gl.DeleteShader(vs);
				gl.DeleteShader(fs);
			}

			private uint Compile(ShaderType type, string src)
			{
				var s = _gl.CreateShader(type);
				_gl.ShaderSource(s, src);
				_gl.CompileShader(s);
				_gl.GetShader(s, ShaderParameterName.CompileStatus, out int status);
				if (status != (int)GLEnum.True)
				{
					throw new InvalidOperationException($"{type} compile failed: " + _gl.GetShaderInfoLog(s));
				}
				return s;
			}

			public void Use() => _gl.UseProgram(_program);

			public unsafe void SetMatrix(string name, Matrix4x4 m)
			{
				var loc = _gl.GetUniformLocation(_program, name);
				if (loc >= 0)
				{
					_gl.UniformMatrix4(loc, 1, false, (float*)&m);
				}
			}

			public void SetVec3(string name, Vector3 v)
			{
				var loc = _gl.GetUniformLocation(_program, name);
				if (loc >= 0)
				{
					_gl.Uniform3(loc, v.X, v.Y, v.Z);
				}
			}

			public void SetInt(string name, int v)
			{
				var loc = _gl.GetUniformLocation(_program, name);
				if (loc >= 0)
				{
					_gl.Uniform1(loc, v);
				}
			}

			public void Dispose() => _gl.DeleteProgram(_program);
		}

		private sealed class Texture : IDisposable
		{
			private readonly GL _gl;
			private readonly uint _handle;

			// The 9-argument gl.TexImage2D overload - one of the calls that aborts under WASM AOT.
			public Texture(GL gl, ReadOnlySpan<byte> rgba, uint width, uint height)
			{
				_gl = gl;
				_handle = gl.GenTexture();
				gl.BindTexture(TextureTarget.Texture2D, _handle);
				gl.TexImage2D<byte>(TextureTarget.Texture2D, 0, InternalFormat.Srgb8Alpha8,
					width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgba);
				gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
				gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
				gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
				gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
				gl.BindTexture(TextureTarget.Texture2D, 0);
			}

			public static Texture CreateWhite(GL gl)
			{
				Span<byte> pixel = stackalloc byte[] { 255, 255, 255, 255 };
				return new Texture(gl, pixel, 1, 1);
			}

			public void Bind(int unit = 0)
			{
				_gl.ActiveTexture(TextureUnit.Texture0 + unit);
				_gl.BindTexture(TextureTarget.Texture2D, _handle);
			}

			public void Dispose() => _gl.DeleteTexture(_handle);
		}

		private sealed class Mesh : IDisposable
		{
			private readonly GL _gl;
			private readonly uint _vao;
			private readonly uint _vbo;
			private readonly uint _ebo;
			private readonly int _indexCount;

			public Vector3 BoundsMin { get; }
			public Vector3 BoundsMax { get; }
			public Texture? DiffuseTexture { get; set; }
			public Vector3 DiffuseColor { get; set; } = Vector3.One;

			public unsafe Mesh(GL gl, ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices)
			{
				_gl = gl;
				_indexCount = indices.Length;

				var min = new Vector3(float.PositiveInfinity);
				var max = new Vector3(float.NegativeInfinity);
				for (int i = 0; i < vertices.Length; i++)
				{
					min = Vector3.Min(min, vertices[i].Position);
					max = Vector3.Max(max, vertices[i].Position);
				}
				BoundsMin = min;
				BoundsMax = max;

				_vao = gl.GenVertexArray();
				gl.BindVertexArray(_vao);

				_vbo = gl.GenBuffer();
				gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
				gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

				_ebo = gl.GenBuffer();
				gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
				gl.BufferData(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

				var stride = (uint)sizeof(Vertex);
				gl.EnableVertexAttribArray(0);
				gl.VertexAttribPointer(0, 3, GLEnum.Float, false, stride, (void*)0);
				gl.EnableVertexAttribArray(1);
				gl.VertexAttribPointer(1, 3, GLEnum.Float, false, stride, (void*)sizeof(Vector3));
				gl.EnableVertexAttribArray(2);
				gl.VertexAttribPointer(2, 2, GLEnum.Float, false, stride, (void*)(sizeof(Vector3) * 2));

				gl.BindVertexArray(0);
			}

			public unsafe void Draw()
			{
				_gl.BindVertexArray(_vao);
				_gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, (void*)0);
			}

			public void Dispose()
			{
				_gl.DeleteVertexArray(_vao);
				_gl.DeleteBuffer(_vbo);
				_gl.DeleteBuffer(_ebo);
			}
		}

		private sealed class Model : IDisposable
		{
			public List<Mesh> Meshes { get; } = new();

			public Vector3 BoundsMin { get; private set; } = new(float.PositiveInfinity);
			public Vector3 BoundsMax { get; private set; } = new(float.NegativeInfinity);

			private void Add(Mesh m)
			{
				Meshes.Add(m);
				BoundsMin = Vector3.Min(BoundsMin, m.BoundsMin);
				BoundsMax = Vector3.Max(BoundsMax, m.BoundsMax);
			}

			public void Dispose()
			{
				foreach (var m in Meshes)
				{
					m.Dispose();
				}
				Meshes.Clear();
			}

			// A warm sphere encircled by a tilted cool ring. Pure System.Numerics math, so it renders
			// identically on Desktop and WebAssembly.
			public static Model CreateShowpiece(GL gl)
			{
				var model = new Model();
				model.Add(BuildSphere(gl, radius: 0.8f, slices: 48, stacks: 32, color: new Vector3(0.85f, 0.35f, 0.30f)));

				var tilt = Quaternion.CreateFromYawPitchRoll(0f, MathF.PI * 0.28f, MathF.PI * 0.12f);
				model.Add(BuildTorus(gl, majorRadius: 1.45f, minorRadius: 0.12f, majorSeg: 96, minorSeg: 20,
					orientation: tilt, color: new Vector3(0.30f, 0.55f, 0.85f)));

				return model;
			}

			private static Mesh BuildSphere(GL gl, float radius, int slices, int stacks, Vector3 color)
			{
				int row = slices + 1;
				var verts = new Vertex[row * (stacks + 1)];
				for (int i = 0; i <= stacks; i++)
				{
					float phi = MathF.PI * i / stacks;
					float sp = MathF.Sin(phi), cp = MathF.Cos(phi);
					for (int j = 0; j <= slices; j++)
					{
						float theta = 2f * MathF.PI * j / slices;
						var n = new Vector3(sp * MathF.Cos(theta), cp, sp * MathF.Sin(theta));
						verts[i * row + j] = new Vertex(n * radius, n, new Vector2((float)j / slices, (float)i / stacks));
					}
				}

				var idx = new uint[stacks * slices * 6];
				int o = 0;
				for (int i = 0; i < stacks; i++)
				{
					for (int j = 0; j < slices; j++)
					{
						uint a = (uint)(i * row + j);
						uint b = a + (uint)row;
						idx[o++] = a; idx[o++] = a + 1; idx[o++] = b;
						idx[o++] = a + 1; idx[o++] = b + 1; idx[o++] = b;
					}
				}

				return new Mesh(gl, verts, idx) { DiffuseColor = color };
			}

			private static Mesh BuildTorus(GL gl, float majorRadius, float minorRadius,
				int majorSeg, int minorSeg, Quaternion orientation, Vector3 color)
			{
				int row = minorSeg + 1;
				var verts = new Vertex[(majorSeg + 1) * row];
				for (int i = 0; i <= majorSeg; i++)
				{
					float u = 2f * MathF.PI * i / majorSeg;
					float cu = MathF.Cos(u), su = MathF.Sin(u);
					for (int j = 0; j <= minorSeg; j++)
					{
						float v = 2f * MathF.PI * j / minorSeg;
						float cv = MathF.Cos(v), sv = MathF.Sin(v);
						var n = new Vector3(cv * cu, sv, cv * su);
						var pos = new Vector3((majorRadius + minorRadius * cv) * cu, minorRadius * sv, (majorRadius + minorRadius * cv) * su);
						pos = Vector3.Transform(pos, orientation);
						n = Vector3.Normalize(Vector3.Transform(n, orientation));
						verts[i * row + j] = new Vertex(pos, n, new Vector2((float)i / majorSeg, (float)j / minorSeg));
					}
				}

				var idx = new uint[majorSeg * minorSeg * 6];
				int o = 0;
				for (int i = 0; i < majorSeg; i++)
				{
					for (int j = 0; j < minorSeg; j++)
					{
						uint a = (uint)(i * row + j);
						uint b = a + (uint)row;
						idx[o++] = a; idx[o++] = a + 1; idx[o++] = b;
						idx[o++] = a + 1; idx[o++] = b + 1; idx[o++] = b;
					}
				}

				return new Mesh(gl, verts, idx) { DiffuseColor = color };
			}
		}

		private sealed class Camera
		{
			public Vector3 Target { get; set; } = Vector3.Zero;
			public float Distance { get; set; } = 5f;
			public float Azimuth { get; set; } = MathF.PI * 0.25f;
			public float Elevation { get; set; } = MathF.PI / 6f;
			public float FieldOfView { get; set; } = MathF.PI / 4f;
			public float NearPlane { get; set; } = 0.05f;
			public float FarPlane { get; set; } = 1000f;

			private Vector3 OrbitEye()
			{
				var cosE = MathF.Cos(Elevation);
				return Target + new Vector3(
					Distance * cosE * MathF.Sin(Azimuth),
					Distance * MathF.Sin(Elevation),
					Distance * cosE * MathF.Cos(Azimuth));
			}

			public Matrix4x4 GetViewMatrix() => Matrix4x4.CreateLookAt(OrbitEye(), Target, Vector3.UnitY);

			public Matrix4x4 GetProjectionMatrix(float aspect) =>
				Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, MathF.Max(0.01f, aspect), NearPlane, FarPlane);

			public void FitToBounds(Vector3 min, Vector3 max)
			{
				var center = (min + max) * 0.5f;
				var radius = MathF.Max(0.01f, (max - min).Length() * 0.5f);
				Target = center;
				Distance = radius / MathF.Sin(FieldOfView * 0.5f) * 1.25f;
				Azimuth = MathF.PI * 0.25f;
				Elevation = MathF.PI / 6f;
			}
		}
	}
}
#endif
