// MIT License
//
// Copyright (c) 2019-2020 Ultz Limited
// Copyright (c) 2021- .NET Foundation and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// https://github.com/dotnet/Silk.NET/tree/c27224cce6b8136224c01d40de2d608879d709b5/examples/CSharp/OpenGL%20Tutorials

#if __SKIA__ || WINAPPSDK
using System;
using System.Diagnostics;
using System.Numerics;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	public class RotatingCubeGlCanvasElement() : GLCanvasElement(() => App.MainWindow)
	{
		private static BufferObject<float> _vbo;
		private static BufferObject<uint> _ebo;
		private static VertexArrayObject<float, uint> _vao;
		private static Shader _shader;

		private static readonly float[] _vertices =
		{
			// Front face     // colors
			0.5f, 0.5f, 0.5f, 1.0f, 0.4f, 0.6f,
			-0.5f, 0.5f, 0.5f, 1.0f, 0.9f, 0.2f,
			-0.5f, -0.5f, 0.5f, 0.7f, 0.3f, 0.8f,
			0.5f, -0.5f, 0.5f, 0.5f, 0.3f, 1.0f,
			// Back face       // colors
			0.5f, 0.5f, -0.5f, 0.2f, 0.6f, 1.0f,
			-0.5f, 0.5f, -0.5f, 0.6f, 1.0f, 0.4f,
			-0.5f, -0.5f, -0.5f, 0.6f, 0.8f, 0.8f,
			0.5f, -0.5f, -0.5f, 0.4f, 0.8f, 0.8f,
		};

		private static readonly uint[] _triangleIndices =
		{
			// Front
			0, 1, 2,
			2, 3, 0,
			// Right
			0, 3, 7,
			7, 4, 0,
			// Bottom
			2, 6, 7,
			7, 3, 2,
			// Left
			1, 5, 6,
			6, 2, 1,
			// Back
			4, 7, 6,
			6, 5, 4,
			// Top
			5, 1, 0,
			0, 4, 5,
		};

		private readonly string _vertexShaderSource =
		"""
		precision highp float; // for OpenGL ES compatibility

		layout(location = 0) in vec3 pos;
		layout(location = 1) in vec3 vertex_color;

		uniform mat4 transform;

		out vec3 color;

		void main() {
		  gl_Position = transform * vec4(pos, 1.0);
		  color = vertex_color;
		}                                      
		""";

		private readonly string FragmentShaderSource =
		"""
		precision highp float; // for OpenGL ES compatibility

		in vec3 color;

		out vec4 frag_color;

		void main() {
		  frag_color = vec4(color, 1.0);
		}                                     
		""";

		protected override void Init(GL Gl)
		{
			_ebo = new BufferObject<uint>(Gl, _triangleIndices, BufferTargetARB.ElementArrayBuffer);
			_vbo = new BufferObject<float>(Gl, _vertices, BufferTargetARB.ArrayBuffer);
			_vao = new VertexArrayObject<float, uint>(Gl, _vbo, _ebo);

			_vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 6, 0);
			_vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 6, 3);

			var slVersion = Gl.GetStringS(StringName.ShadingLanguageVersion);
			var versionDef = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase)
				? "#version 300 es"
				: "#version 330";
			_shader = new Shader(Gl, versionDef + Environment.NewLine + _vertexShaderSource, versionDef + Environment.NewLine + FragmentShaderSource);
		}

		protected override void OnDestroy(GL Gl)
		{
			_vbo.Dispose();
			_ebo.Dispose();
			_vao.Dispose();
			_shader.Dispose();
		}

		// somewhat follows https://github.com/c2d7fa/opengl-cube
		protected override unsafe void RenderOverride(GL Gl)
		{
			Gl.Enable(EnableCap.DepthTest);
			Gl.DepthMask(true);

			Gl.ClearColor(0.1f, 0.12f, 0.2f, 1);
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			_vao.Bind();
			_shader.Use();

			const double duration = 4;
			var transform =
				Matrix4x4.CreateRotationY((float)((Stopwatch.GetElapsedTime(0).TotalSeconds / duration) * (2 * Math.PI))) *
				Matrix4x4.CreateRotationX((float)(0.15 * Math.PI)) *
				Matrix4x4.CreateTranslation(0, 0, -3) *
				Perspective();

			_shader.SetUniform("transform", transform);
			Gl.DrawElements(PrimitiveType.Triangles, (uint)_triangleIndices.Length, DrawElementsType.UnsignedInt, null);

			// https://www.songho.ca/opengl/gl_projectionmatrix.html
			static Matrix4x4 Perspective()
			{
				const float
					r = 0.5f, // Half of the viewport width (at the near plane)
					t = 0.5f, // Half of the viewport height (at the near plane)
					n = 1, // Distance to near clipping plane
					f = 5; // Distance to far clipping plane

				return new Matrix4x4(
					n / r, 0, 0, 0,
					0, n / t, 0, 0,
					0, 0, (-f - n) / (f - n), -1,
					0, 0, (2 * f * n) / (n - f), 0
				);
			}

			Invalidate(); // continuous redrawing
		}

		public class Shader : IDisposable
		{
			private readonly uint _handle;
			private readonly GL _gl;

			public Shader(GL gl, string vertexShaderSource, string fragmentShaderSource)
			{
				_gl = gl;

				uint vertex = LoadShader(ShaderType.VertexShader, vertexShaderSource);
				uint fragment = LoadShader(ShaderType.FragmentShader, fragmentShaderSource);
				_handle = _gl.CreateProgram();
				_gl.AttachShader(_handle, vertex);
				_gl.AttachShader(_handle, fragment);
				_gl.LinkProgram(_handle);
				_gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
				if (status == 0)
				{
					throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");
				}
				_gl.DetachShader(_handle, vertex);
				_gl.DetachShader(_handle, fragment);
				_gl.DeleteShader(vertex);
				_gl.DeleteShader(fragment);
			}

			public void Use()
			{
				_gl.UseProgram(_handle);
			}

			public void SetUniform(string name, int value)
			{
				int location = _gl.GetUniformLocation(_handle, name);
				if (location == -1)
				{
					throw new Exception($"{name} uniform not found on shader.");
				}
				_gl.Uniform1(location, value);
			}

			public unsafe void SetUniform(string name, Matrix4x4 value)
			{
				//A new overload has been created for setting a uniform so we can use the transform in our shader.
				int location = _gl.GetUniformLocation(_handle, name);
				if (location == -1)
				{
					throw new Exception($"{name} uniform not found on shader.");
				}
				_gl.UniformMatrix4(location, 1, false, (float*)&value);
			}

			public void SetUniform(string name, float value)
			{
				int location = _gl.GetUniformLocation(_handle, name);
				if (location == -1)
				{
					throw new Exception($"{name} uniform not found on shader.");
				}
				_gl.Uniform1(location, value);
			}

			public void Dispose()
			{
				_gl.DeleteProgram(_handle);
			}

			private uint LoadShader(ShaderType type, string src)
			{
				uint handle = _gl.CreateShader(type);
				_gl.ShaderSource(handle, src);
				_gl.CompileShader(handle);
				string infoLog = _gl.GetShaderInfoLog(handle);
				if (!string.IsNullOrWhiteSpace(infoLog))
				{
					throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
				}

				return handle;
			}
		}

		public class BufferObject<TDataType> : IDisposable where TDataType : unmanaged
		{
			private readonly uint _handle;
			private readonly BufferTargetARB _bufferType;
			private readonly GL _gl;

			public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
			{
				_gl = gl;
				_bufferType = bufferType;

				_handle = _gl.GenBuffer();
				Bind();
				fixed (void* d = data)
				{
					_gl.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
				}
			}

			public void Bind() => _gl.BindBuffer(_bufferType, _handle);
			public void Dispose() => _gl.DeleteBuffer(_handle);
		}

		public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
			where TVertexType : unmanaged
			where TIndexType : unmanaged
		{
			private readonly uint _handle;
			private readonly GL _gl;

			public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
			{
				_gl = gl;

				_handle = _gl.GenVertexArray();
				Bind();
				vbo.Bind();
				ebo.Bind();
			}

			public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
			{
				_gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexType), (void*)(offSet * sizeof(TVertexType)));
				_gl.EnableVertexAttribArray(index);
			}

			public void Bind() => _gl.BindVertexArray(_handle);
			public void Dispose() => _gl.DeleteVertexArray(_handle);
		}
	}
}
#endif
