#if __SKIA__ || WINAPPSDK
using System;
using SamplesApp;
using Silk.NET.OpenGL;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DGL;

namespace UITests.Shared.Windows_UI_Composition
{
	// Stress tests compressed-texture uploads and framebuffer-to-texture copies:
	//   - gl.CompressedTexImage2D + gl.CompressedTexSubImage2D (ETC2; mandatory in real ES3,
	//     but on WebGL2 only available via WEBGL_compressed_texture_etc - absent on most
	//     desktop browsers, so the uncompressed fallback path is the common one there)
	//   - gl.CompressedTexImage3D + gl.CompressedTexSubImage3D on a TEXTURE_2D_ARRAY
	//   - gl.CopyTexImage2D / gl.CopyTexSubImage2D / gl.CopyTexSubImage3D from an offscreen FBO
	//   - gl.FramebufferTextureLayer (rendering into one layer of an array texture)
	//   - gl.InvalidateSubFramebuffer
	//   - gl.GetStringi (extension enumeration for the ETC2 feature check on desktop GL)
	//
	// 2x2 grid: [ETC2 2D texture | ETC2 array layer (cycling)] over [CopyTexImage2D snapshot |
	// array layer combining CopyTexSubImage3D and a FramebufferTextureLayer render]. Where ETC2
	// isn't available (desktop GL < 4.3 without ARB_ES3_compatibility, or WebGL2 without
	// WEBGL_compressed_texture_etc) the compressed quadrants fall back to uncompressed
	// uploads (logged).
	public class GLCanvasElement_CompressedCopyElement() : GLCanvasElement(() => App.MainWindow)
	{
		private const int TexSize = 32; // multiple of the 4x4 ETC2 block size
		private const int LayerCount = 2;

		private uint _vao, _vbo, _program;
		private int _uTex2DLoc, _uTexArrayLoc, _uLayerLoc;
		private uint _etc2Tex, _etc2Array;
		private uint _copyTex, _copyArray;
		private uint _scratchFbo, _scratchColor, _layerFbo;
		private uint _sceneVao, _sceneVbo, _sceneProgram;
		private int _sceneUTimeLoc;
		private DateTime _startTime;

		protected override unsafe void Init(GL gl)
		{
			_startTime = DateTime.UtcNow;

			var etc2Supported = DetectEtc2Support(gl);

			// --- ETC2 2D texture: red base, green center via CompressedTexSubImage2D ---
			_etc2Tex = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, _etc2Tex);
			if (etc2Supported)
			{
				var red = MakeEtc2Rgb8Data(TexSize, TexSize, 200, 30, 30);
				fixed (byte* p = red)
				{
					gl.CompressedTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.CompressedRgb8Etc2, TexSize, TexSize, 0, (uint)red.Length, p);
				}
				var green = MakeEtc2Rgb8Data(TexSize / 2, TexSize / 2, 30, 200, 30);
				fixed (byte* p = green)
				{
					gl.CompressedTexSubImage2D(TextureTarget.Texture2D, 0, TexSize / 4, TexSize / 4, TexSize / 2, TexSize / 2, GLEnum.CompressedRgb8Etc2, (uint)green.Length, p);
				}
			}
			else
			{
				UploadSolidFallback(gl, TextureTarget.Texture2D, 200, 30, 30);
			}
			SetNearestFilters(gl, TextureTarget.Texture2D);

			// --- ETC2 array texture: blue and yellow layers, partially overwritten per Init ---
			_etc2Array = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2DArray, _etc2Array);
			if (etc2Supported)
			{
				var layers = new byte[2][] { MakeEtc2Rgba8Data(TexSize, TexSize, 40, 40, 220), MakeEtc2Rgba8Data(TexSize, TexSize, 220, 220, 40) };
				var all = new byte[layers[0].Length * LayerCount];
				layers[0].CopyTo(all, 0);
				layers[1].CopyTo(all, layers[0].Length);
				fixed (byte* p = all)
				{
					gl.CompressedTexImage3D(TextureTarget.Texture2DArray, 0, InternalFormat.CompressedRgba8Etc2Eac, TexSize, TexSize, LayerCount, 0, (uint)all.Length, p);
				}
				// Overwrite the top half of layer 1 with magenta.
				var magenta = MakeEtc2Rgba8Data(TexSize, TexSize / 2, 220, 40, 220);
				fixed (byte* p = magenta)
				{
					gl.CompressedTexSubImage3D(TextureTarget.Texture2DArray, 0, 0, TexSize / 2, 1, TexSize, TexSize / 2, 1, GLEnum.CompressedRgba8Etc2Eac, (uint)magenta.Length, p);
				}
			}
			else
			{
				gl.TexStorage3D(TextureTarget.Texture2DArray, 1, SizedInternalFormat.Rgba8, TexSize, TexSize, LayerCount);
				UploadSolidLayerFallback(gl, 0, 40, 40, 220);
				UploadSolidLayerFallback(gl, 1, 220, 220, 40);
			}
			SetNearestFilters(gl, TextureTarget.Texture2DArray);

			// --- Copy destinations ---
			_copyTex = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, _copyTex);
			SetNearestFilters(gl, TextureTarget.Texture2D);

			_copyArray = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2DArray, _copyArray);
			gl.TexStorage3D(TextureTarget.Texture2DArray, 1, SizedInternalFormat.Rgba8, TexSize, TexSize, LayerCount);
			SetNearestFilters(gl, TextureTarget.Texture2DArray);

			// --- Scratch FBO the copy sources are rendered into ---
			_scratchColor = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, _scratchColor);
			gl.TexStorage2D(TextureTarget.Texture2D, 1, SizedInternalFormat.Rgba8, TexSize, TexSize);
			SetNearestFilters(gl, TextureTarget.Texture2D);
			_scratchFbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, _scratchFbo);
			gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, _scratchColor, 0);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("Scratch FBO is not complete");
			}

			// --- FBO targeting layer 0 of the copy-dest array via FramebufferTextureLayer ---
			_layerFbo = gl.GenFramebuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, _layerFbo);
			gl.FramebufferTextureLayer(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, _copyArray, 0, 0);
			if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
			{
				throw new Exception("Layer FBO is not complete");
			}

			// --- Geometry + display shaders ---
			var slVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
			var isEs = slVersion.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase);
			var versionDef = isEs ? "#version 300 es" : "#version 330";
			var samplerPrecision = isEs ? "precision highp sampler2DArray;" : "";

			var quad = new float[]
			{
				-1f, -1f,     0f, 0f,
				 1f, -1f,     1f, 0f,
				 1f,  1f,     1f, 1f,
				-1f, -1f,     0f, 0f,
				 1f,  1f,     1f, 1f,
				-1f,  1f,     0f, 1f,
			};
			_vao = gl.GenVertexArray();
			gl.BindVertexArray(_vao);
			_vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(quad), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);
			gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
			gl.EnableVertexAttribArray(1);

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
				uniform sampler2D uTex2D;
				uniform sampler2DArray uTexArray;
				uniform float uLayer;
				void main() {
				    // 2x2 grid: TL = uTex2D, TR = uTexArray layer uLayer,
				    //           BL = uTex2D (copy view), BR = uTexArray (copy view).
				    vec2 cell = vec2(fract(vUV.x * 2.0), fract(vUV.y * 2.0));
				    if (vUV.y >= 0.5) {
				        if (vUV.x < 0.5) fragColor = texture(uTex2D, cell);
				        else fragColor = texture(uTexArray, vec3(cell, uLayer));
				    } else {
				        if (vUV.x < 0.5) fragColor = texture(uTex2D, cell);
				        else fragColor = texture(uTexArray, vec3(cell, uLayer));
				    }
				}
				""");
			_uTex2DLoc = gl.GetUniformLocation(_program, "uTex2D");
			_uTexArrayLoc = gl.GetUniformLocation(_program, "uTexArray");
			_uLayerLoc = gl.GetUniformLocation(_program, "uLayer");

			// Tiny scene rendered into the scratch FBO as the copy source.
			_sceneVao = gl.GenVertexArray();
			gl.BindVertexArray(_sceneVao);
			_sceneVbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, _sceneVbo);
			var tri = new float[] { 0f, 0.9f, -0.9f, -0.9f, 0.9f, -0.9f };
			gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(tri), BufferUsageARB.StaticDraw);
			gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 2 * sizeof(float), (void*)0);
			gl.EnableVertexAttribArray(0);

			_sceneProgram = CreateProgram(gl,
				versionDef + """

				precision highp float;
				layout(location = 0) in vec2 aPos;
				uniform float uTime;
				out vec2 vPos;
				void main() {
				    float c = cos(uTime), s = sin(uTime);
				    gl_Position = vec4(mat2(c, -s, s, c) * aPos, 0.0, 1.0);
				    vPos = aPos;
				}
				""",
				versionDef + """

				precision highp float;
				in vec2 vPos;
				out vec4 fragColor;
				void main() { fragColor = vec4(0.5 + 0.5 * vPos, 1.0, 1.0); }
				""");
			_sceneUTimeLoc = gl.GetUniformLocation(_sceneProgram, "uTime");
		}

		private unsafe bool DetectEtc2Support(GL gl)
		{
			var version = gl.GetStringS(StringName.Version);

			if (version.Contains("WebGL", StringComparison.InvariantCultureIgnoreCase))
			{
				// Unlike real ES 3.0, WebGL 2.0 does NOT guarantee ETC2/EAC: desktop GPUs can't
				// decode it, so browsers only expose it via WEBGL_compressed_texture_etc
				// (typically present on mobile, absent on desktop).
				if (HasExtension(gl, "WEBGL_compressed_texture_etc"))
				{
					return true;
				}
			}
			else if (version.Contains("OpenGL ES", StringComparison.InvariantCultureIgnoreCase))
			{
				return true; // ETC2 is mandatory in (real) ES 3.0
			}
			else
			{
				// Desktop GL: core since 4.3, otherwise advertised via ARB_ES3_compatibility.
				gl.GetInteger(GLEnum.MajorVersion, out int major);
				gl.GetInteger(GLEnum.MinorVersion, out int minor);
				if (major > 4 || (major == 4 && minor >= 3) || HasExtension(gl, "GL_ARB_ES3_compatibility"))
				{
					return true;
				}
			}

			Console.WriteLine("ETC2 compressed textures are not supported on this GL; using uncompressed fallbacks.");
			return false;
		}

		private static bool HasExtension(GL gl, string name)
		{
			gl.GetInteger(GLEnum.NumExtensions, out int extensionCount);
			for (uint i = 0; i < extensionCount; i++)
			{
				if (gl.GetStringS(StringName.Extensions, i) == name)
				{
					return true;
				}
			}
			return false;
		}

		// A solid-color ETC2 RGB8 block (individual mode, modifier tables 0, all pixel indices 0).
		private static byte[] MakeEtc2Rgb8Data(int width, int height, byte r, byte g, byte b)
		{
			var blocks = (width / 4) * (height / 4);
			var data = new byte[blocks * 8];
			for (int i = 0; i < blocks; i++)
			{
				data[i * 8 + 0] = (byte)(r & 0xF0 | (r >> 4));
				data[i * 8 + 1] = (byte)(g & 0xF0 | (g >> 4));
				data[i * 8 + 2] = (byte)(b & 0xF0 | (b >> 4));
				// bytes 3..7 = 0: diff/flip 0 (individual mode), tables 0, all indices 0
			}
			return data;
		}

		// A solid ETC2+EAC RGBA8 block: 8 bytes of EAC alpha (base 255) + the RGB8 block.
		private static byte[] MakeEtc2Rgba8Data(int width, int height, byte r, byte g, byte b)
		{
			var rgb = MakeEtc2Rgb8Data(width, height, r, g, b);
			var blocks = rgb.Length / 8;
			var data = new byte[blocks * 16];
			for (int i = 0; i < blocks; i++)
			{
				data[i * 16 + 0] = 255; // EAC base alpha; multiplier/table/indices 0
				Array.Copy(rgb, i * 8, data, i * 16 + 8, 8);
			}
			return data;
		}

		private static unsafe void UploadSolidFallback(GL gl, TextureTarget target, byte r, byte g, byte b)
		{
			var pixels = new byte[TexSize * TexSize * 4];
			for (int i = 0; i < TexSize * TexSize; i++)
			{
				pixels[i * 4] = r; pixels[i * 4 + 1] = g; pixels[i * 4 + 2] = b; pixels[i * 4 + 3] = 255;
			}
			fixed (byte* p = pixels)
			{
				gl.TexImage2D(target, 0, InternalFormat.Rgba8, TexSize, TexSize, 0, GLEnum.Rgba, GLEnum.UnsignedByte, p);
			}
		}

		private static unsafe void UploadSolidLayerFallback(GL gl, int layer, byte r, byte g, byte b)
		{
			var pixels = new byte[TexSize * TexSize * 4];
			for (int i = 0; i < TexSize * TexSize; i++)
			{
				pixels[i * 4] = r; pixels[i * 4 + 1] = g; pixels[i * 4 + 2] = b; pixels[i * 4 + 3] = 255;
			}
			fixed (byte* p = pixels)
			{
				gl.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, layer, TexSize, TexSize, 1, GLEnum.Rgba, GLEnum.UnsignedByte, p);
			}
		}

		private static void SetNearestFilters(GL gl, TextureTarget target)
		{
			gl.TexParameterI(target, GLEnum.TextureMinFilter, (uint)GLEnum.Nearest);
			gl.TexParameterI(target, GLEnum.TextureMagFilter, (uint)GLEnum.Nearest);
			gl.TexParameterI(target, GLEnum.TextureWrapS, (uint)GLEnum.ClampToEdge);
			gl.TexParameterI(target, GLEnum.TextureWrapT, (uint)GLEnum.ClampToEdge);
		}

		protected override void OnDestroy(GL gl)
		{
			gl.DeleteVertexArray(_vao);
			gl.DeleteBuffer(_vbo);
			gl.DeleteProgram(_program);
			gl.DeleteVertexArray(_sceneVao);
			gl.DeleteBuffer(_sceneVbo);
			gl.DeleteProgram(_sceneProgram);
			gl.DeleteTexture(_etc2Tex);
			gl.DeleteTexture(_etc2Array);
			gl.DeleteTexture(_copyTex);
			gl.DeleteTexture(_copyArray);
			gl.DeleteTexture(_scratchColor);
			gl.DeleteFramebuffer(_scratchFbo);
			gl.DeleteFramebuffer(_layerFbo);
		}

		protected override unsafe void RenderOverride(GL gl)
		{
			var t = (float)(DateTime.UtcNow - _startTime).TotalSeconds;

			gl.GetInteger(GLEnum.FramebufferBinding, out int frameworkFbo);

			// --- Render the spinning copy source into the scratch FBO ---
			gl.BindFramebuffer(GLEnum.Framebuffer, _scratchFbo);
			gl.Viewport(0, 0, TexSize, TexSize);
			gl.ClearColor(0.1f, 0.1f, 0.15f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_sceneProgram);
			gl.Uniform1(_sceneUTimeLoc, t);
			gl.BindVertexArray(_sceneVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			// --- Copies read from the currently bound READ framebuffer (the scratch FBO) ---
			gl.BindTexture(TextureTarget.Texture2D, _copyTex);
			gl.CopyTexImage2D(TextureTarget.Texture2D, 0, GLEnum.Rgba8, 0, 0, TexSize, TexSize, 0);
			// Refresh just the center region too, exercising the sub-copy path.
			gl.CopyTexSubImage2D(TextureTarget.Texture2D, 0, TexSize / 4, TexSize / 4, TexSize / 4, TexSize / 4, TexSize / 2, TexSize / 2);

			// Copy into layer 1 of the array; layer 0 is rendered into directly below.
			gl.BindTexture(TextureTarget.Texture2DArray, _copyArray);
			gl.CopyTexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, 1, 0, 0, TexSize, TexSize);

			// The scratch contents are consumed; hint that the center region can be dropped.
			ReadOnlySpan<GLEnum> attachments = stackalloc GLEnum[] { GLEnum.ColorAttachment0 };
			gl.InvalidateSubFramebuffer(GLEnum.ReadFramebuffer, (uint)attachments.Length, attachments, TexSize / 4, TexSize / 4, TexSize / 2, TexSize / 2);

			// --- Render a counter-spinning source directly into array layer 0 via FramebufferTextureLayer ---
			gl.BindFramebuffer(GLEnum.Framebuffer, _layerFbo);
			gl.Viewport(0, 0, TexSize, TexSize);
			gl.ClearColor(0.15f, 0.1f, 0.1f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_sceneProgram);
			gl.Uniform1(_sceneUTimeLoc, -t);
			gl.BindVertexArray(_sceneVao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			// --- Composite the 2x2 grid onto the framework target ---
			gl.BindFramebuffer(GLEnum.Framebuffer, (uint)frameworkFbo);
			gl.Viewport(0, 0, (uint)RenderSize.Width, (uint)RenderSize.Height);
			gl.ClearColor(0f, 0f, 0f, 1f);
			gl.Clear(ClearBufferMask.ColorBufferBit);
			gl.UseProgram(_program);
			gl.ActiveTexture(TextureUnit.Texture0);
			// Top row shows the ETC2 textures, bottom row the copy results - swap per second half.
			var showCopies = ((int)t) % 2 == 1;
			gl.BindTexture(TextureTarget.Texture2D, showCopies ? _copyTex : _etc2Tex);
			gl.ActiveTexture(TextureUnit.Texture1);
			gl.BindTexture(TextureTarget.Texture2DArray, showCopies ? _copyArray : _etc2Array);
			gl.Uniform1(_uTex2DLoc, 0);
			gl.Uniform1(_uTexArrayLoc, 1);
			gl.Uniform1(_uLayerLoc, (float)(((int)(t * 2)) % LayerCount));
			gl.BindVertexArray(_vao);
			gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
			gl.ActiveTexture(TextureUnit.Texture0);

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
