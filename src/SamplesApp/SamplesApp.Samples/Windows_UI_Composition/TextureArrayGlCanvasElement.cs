#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests texture storage, 2D-array textures, and sampler objects:
	//   - gl.TexStorage3D (immutable TEXTURE_2D_ARRAY storage with a full mip chain)
	//   - gl.TexSubImage3D per-layer upload (routed through the >8-arg C shim on wasm)
	//   - gl.GenerateMipmap on the array texture
	//   - gl.TexStorage2D + per-frame gl.TexSubImage2D (also a C shim) on a dynamic texture
	//   - gl.PixelStorei (UNPACK_ALIGNMENT for tightly-packed uploads)
	//   - sampler objects: gl.GenSamplers / gl.SamplerParameter / gl.BindSampler / gl.DeleteSampler
	//
	// Left half: a 4-layer checkerboard array texture cycling through layers, minified (UV x8)
	// so the sampler object's mip mode is visible - it alternates between NEAREST and trilinear
	// every few seconds. Right half: a 2D texture with a color band scrolled one row per frame
	// via TexSubImage2D.
	public class GLCanvasElement_TextureArrayElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int TexSize = 64;
		private const int LayerCount = 4;

		private uint _vao, _vbo, _program;
		private uint _arrayTexture, _dynamicTexture;
		private uint _samplerNearest, _samplerTrilinear;
		private int _uArrayTexLoc, _uDynamicTexLoc, _uLayerLoc;
		private int _scrollRow;
		private DateTime _startTime;

		private static readonly float[] _quad =
		{
			// pos        // uv
			-1f, -1f,     0f, 0f,
			 1f, -1f,     1f, 0f,
			 1f,  1f,     1f, 1f,
			-1f, -1f,     0f, 0f,
			 1f,  1f,     1f, 1f,
			-1f,  1f,     0f, 1f,
		};

		private static readonly (byte r, byte g, byte b)[] _layerColors =
		{
			(230,  80,  80),
			( 80, 230,  80),
			( 80,  80, 230),
			(230, 230,  80),
		};

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

			// --- 2D-array texture: immutable storage, per-layer checkerboards, full mip chain ---
			_arrayTexture = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2DArray, _arrayTexture);
			var mipLevels = (uint)(Math.Log2(TexSize) + 1);
			gl.TexStorage3D(TextureTarget.Texture2DArray, mipLevels, SizedInternalFormat.Rgba8, TexSize, TexSize, LayerCount);

			var pixels = new byte[TexSize * TexSize * 4];
			for (int layer = 0; layer < LayerCount; layer++)
			{
				var (r, g, b) = _layerColors[layer];
				for (int y = 0; y < TexSize; y++)
				{
					for (int x = 0; x < TexSize; x++)
					{
						var check = ((x / 8) + (y / 8)) % 2 == 0;
						var i = (y * TexSize + x) * 4;
						pixels[i + 0] = check ? r : (byte)25;
						pixels[i + 1] = check ? g : (byte)25;
						pixels[i + 2] = check ? b : (byte)25;
						pixels[i + 3] = 255;
					}
				}
				fixed (byte* p = pixels)
				{
					gl.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, layer, TexSize, TexSize, 1, GLEnum.Rgba, GLEnum.UnsignedByte, p);
				}
			}
			gl.GenerateMipmap(TextureTarget.Texture2DArray);

			// --- Sampler objects: filtering/wrap state decoupled from the texture ---
			_samplerNearest = gl.GenSampler();
			gl.SamplerParameter(_samplerNearest, SamplerParameterI.MinFilter, (int)GLEnum.NearestMipmapNearest);
			gl.SamplerParameter(_samplerNearest, SamplerParameterI.MagFilter, (int)GLEnum.Nearest);
			gl.SamplerParameter(_samplerNearest, SamplerParameterI.WrapS, (int)GLEnum.Repeat);
			gl.SamplerParameter(_samplerNearest, SamplerParameterI.WrapT, (int)GLEnum.Repeat);

			_samplerTrilinear = gl.GenSampler();
			gl.SamplerParameter(_samplerTrilinear, SamplerParameterI.MinFilter, (int)GLEnum.LinearMipmapLinear);
			gl.SamplerParameter(_samplerTrilinear, SamplerParameterI.MagFilter, (int)GLEnum.Linear);
			gl.SamplerParameter(_samplerTrilinear, SamplerParameterI.WrapS, (int)GLEnum.Repeat);
			gl.SamplerParameter(_samplerTrilinear, SamplerParameterI.WrapT, (int)GLEnum.Repeat);

			// --- Dynamic 2D texture: scrolled one row per frame via TexSubImage2D ---
			_dynamicTexture = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, _dynamicTexture);
			gl.TexStorage2D(TextureTarget.Texture2D, 1, SizedInternalFormat.Rgba8, TexSize, TexSize);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Linear);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Nearest);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapS, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(TextureTarget.Texture2D, GLEnum.TextureWrapT, (uint)GLEnum.ClampToEdge);

			// --- Geometry + shaders ---
			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_quad), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);

			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var isEs = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase);
			var versionDef = isEs ? "#version 300 es" : "#version 330";
			// Sampler precision statements are an ES-only construct; strict desktop drivers
			// reject them in #version 330.
			var samplerPrecision = isEs ? "precision highp sampler2DArray;" : "";

			_program = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				layout(location = 1) in vec2 aUV;
				out vec2 vUV;
				void main() {
				    gl_Position = vec4(aPos, 0.0, 1.0);
				    vUV = aUV;
				}
				""",
				versionDef + "\n" + samplerPrecision + """

				precision highp float;
				in vec2 vUV;
				out vec4 fragColor;
				uniform sampler2DArray uArrayTex;
				uniform sampler2D uDynamicTex;
				uniform float uLayer;
				void main() {
				    if (vUV.x < 0.5) {
				        // Minified (x8) so the sampler's mip filtering is visible.
				        fragColor = texture(uArrayTex, vec3(vUV * 8.0, uLayer));
				    } else {
				        fragColor = texture(uDynamicTex, vec2((vUV.x - 0.5) * 2.0, vUV.y));
				    }
				}
				""");
			_uArrayTexLoc = gl.GetUniformLocation(_program, "uArrayTex");
			_uDynamicTexLoc = gl.GetUniformLocation(_program, "uDynamicTex");
			_uLayerLoc = gl.GetUniformLocation(_program, "uLayer");
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteTexture(_arrayTexture);
			gl.DeleteTexture(_dynamicTexture);
			gl.DeleteSampler(_samplerNearest);
			gl.DeleteSampler(_samplerTrilinear);
			gl.DeleteProgram(_program);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			// --- Scroll one rainbow row into the dynamic texture each frame ---
			var row = new byte[TexSize * 4];
			for (int x = 0; x < TexSize; x++)
			{
				var hue = (x / (float)TexSize + t * 0.2f) % 1f;
				var (r, g, b) = HueToRgb(hue);
				row[x * 4 + 0] = r;
				row[x * 4 + 1] = g;
				row[x * 4 + 2] = b;
				row[x * 4 + 3] = 255;
			}
			gl.BindTexture(TextureTarget.Texture2D, _dynamicTexture);
			fixed (byte* p = row)
			{
				gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, _scrollRow, TexSize, 1, GLEnum.Rgba, GLEnum.UnsignedByte, p);
			}
			_scrollRow = (_scrollRow + 1) % TexSize;

			gl.ClearColor(0.05f, 0.05f, 0.07f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);

			gl.UseProgram(_program);

			// Cycle layers once per second; swap the sampler object every full layer cycle.
			var layer = (int)t % LayerCount;
			var trilinear = ((int)t / LayerCount) % 2 == 1;
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2DArray, _arrayTexture);
			gl.BindSampler(0, trilinear ? _samplerTrilinear : _samplerNearest);
			gl.ActiveTexture(TextureUnit.Texture1);
			gl.BindTexture(TextureTarget.Texture2D, _dynamicTexture);
			gl.Uniform1(_uArrayTexLoc, 0);
			gl.Uniform1(_uDynamicTexLoc, 1);
			gl.Uniform1(_uLayerLoc, (float)layer);

			gl.BindVertexArray(_vao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

			// Unbind the sampler so the unit's default texture state is back in effect.
			gl.BindSampler(0, 0);
			gl.ActiveTexture(TextureUnit.Texture0);

			Invalidate();
		}

		private static (byte, byte, byte) HueToRgb(float hue)
		{
			var h = hue * 6f;
			var x = (byte)(255 * (1 - Math.Abs(h % 2 - 1)));
			return ((int)h % 6) switch
			{
				0 => ((byte)255, x, (byte)0),
				1 => (x, (byte)255, (byte)0),
				2 => ((byte)0, (byte)255, x),
				3 => ((byte)0, x, (byte)255),
				4 => (x, (byte)0, (byte)255),
				_ => ((byte)255, (byte)0, x),
			};
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
