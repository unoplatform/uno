// Minimal-but-real WebGPU backend implementing the NEUTRAL drawing seam (public SPI from Uno.UI.Composition).
// Solid rects + even-odd path fill (stencil-then-cover) consuming IGeometry.StreamFlattened (Skia-less).
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe class WebGpuDevice : IDisposable
{
	public readonly WebGPU W;
	public readonly Wgpu Native;
	public Instance* Inst;
	public Adapter* Adapter;
	public Device* Dev;
	public Queue* Q;
	public RenderPipeline* SolidPipe;
	public RenderPipeline* StencilEvenOdd;
	public RenderPipeline* StencilNonZero;
	public RenderPipeline* CoverPipe;
	public RenderPipeline* ImagePipe;
	public RenderPipeline* GradientPipe;
	public BindGroupLayout* ImgBgl;
	public BindGroupLayout* GradBgl;
	public Sampler* Smp;

	// Uniform size (bytes) of the gradient struct: header(16) + geo(16) + colors(16*16) + stops(4*16).
	public const int GradientUniformBytes = 16 + 16 + 16 * 16 + 4 * 16;
	public const int MaxGradientStops = 16;

	// Multisample count for anti-aliasing. Every pipeline + the color/depth render targets use this; the pass
	// renders into a multisampled color texture that resolves into the single-sample present/readback texture.
	public const uint MsaaSamples = 4;

	// The color-attachment format the pipelines + offscreen targets use. Rgba8Unorm by default (the
	// offscreen/readback path assumes it); a swapchain renderer passes the surface's supported format.
	public readonly TextureFormat ColorFormat;
	public const TextureFormat DefaultColorFormat = TextureFormat.Rgba8Unorm;
	public const TextureFormat DepthStencilFormat = TextureFormat.Depth24PlusStencil8;

	public WebGpuDevice(TextureFormat colorFormat = DefaultColorFormat)
	{
		ColorFormat = colorFormat;
		W = WebGPU.GetApi();
		W.TryGetDeviceExtension(null, out Native);
		var idesc = new InstanceDescriptor();
		Inst = W.CreateInstance(ref idesc);
		var aopts = new RequestAdapterOptions { PowerPreference = PowerPreference.HighPerformance };
		W.InstanceRequestAdapter(Inst, in aopts, new PfnRequestAdapterCallback((s, a, m, _) => Adapter = a), null);
		var ddesc = new DeviceDescriptor();
		W.AdapterRequestDevice(Adapter, in ddesc, new PfnRequestDeviceCallback((s, d, m, _) => Dev = d), null);
		Q = W.DeviceGetQueue(Dev);
		CreatePipelines();
	}

	/// <summary>Reads a surface's resolved single-sample texture back to CPU as tightly-packed RGBA8 (top-down). For RTB and tests.</summary>
	public byte[] ReadPixelsRgba(WebGpuRenderSurface s)
	{
		int w = s.Width, h = s.Height;
		uint unpadded = (uint)(w * 4);
		uint padded = (unpadded + 255u) & ~255u;              // wgpu requires 256-byte row alignment for T2B copies
		ulong total = (ulong)padded * (uint)h;
		var bd = new BufferDescriptor { Size = (nuint)total, Usage = BufferUsage.CopyDst | BufferUsage.MapRead };
		var buf = W.DeviceCreateBuffer(Dev, ref bd);
		var enc = W.DeviceCreateCommandEncoder(Dev, null);
		var src = new ImageCopyTexture { Texture = s.Tex, Aspect = TextureAspect.All, MipLevel = 0, Origin = default };
		var dst = new ImageCopyBuffer { Buffer = buf, Layout = new TextureDataLayout { Offset = 0, BytesPerRow = padded, RowsPerImage = (uint)h } };
		var ext = new Extent3D((uint)w, (uint)h, 1);
		W.CommandEncoderCopyTextureToBuffer(enc, in src, in dst, in ext);
		var cb = W.CommandEncoderFinish(enc, null);
		W.QueueSubmit(Q, 1, &cb);
		Native.DevicePoll(Dev, true, null);

		bool mapped = false;
		W.BufferMapAsync(buf, MapMode.Read, 0, (nuint)total, new PfnBufferMapCallback((status, _) => mapped = true), null);
		while (!mapped) { Native.DevicePoll(Dev, true, null); }
		var mp = (byte*)W.BufferGetMappedRange(buf, 0, (nuint)total);
		var outp = new byte[w * h * 4];
		for (int y = 0; y < h; y++)
		{
			for (uint x = 0; x < unpadded; x++) { outp[y * (int)unpadded + (int)x] = mp[(uint)y * padded + x]; }
		}
		W.BufferUnmap(buf);
		W.BufferDestroy(buf);
		return outp;
	}

	private const string ColoredWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>) -> VOut {
  var o: VOut; o.p = vec4<f32>(pos, 0.0, 1.0); o.c = col; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return i.c; }";

	private const string PosOnlyWgsl = @"
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return vec4<f32>(pos, 0.0, 1.0); }
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";

	private ShaderModule* Module(string wgsl)
	{
		var code = (byte*)SilkMarshal.StringToPtr(wgsl, NativeStringEncoding.UTF8);
		var w = new ShaderModuleWGSLDescriptor { Chain = new ChainedStruct { SType = SType.ShaderModuleWgslDescriptor }, Code = code };
		var d = new ShaderModuleDescriptor { NextInChain = (ChainedStruct*)&w };
		return W.DeviceCreateShaderModule(Dev, ref d);
	}

	private static StencilFaceState Face(CompareFunction cmp, StencilOperation pass)
		=> new() { Compare = cmp, FailOp = StencilOperation.Keep, DepthFailOp = StencilOperation.Keep, PassOp = pass };

	private void CreatePipelines()
	{
		var colored = Module(ColoredWgsl);
		var posOnly = Module(PosOnlyWgsl);
		var vs = (byte*)SilkMarshal.StringToPtr("vs", NativeStringEncoding.UTF8);
		var fs = (byte*)SilkMarshal.StringToPtr("fs", NativeStringEncoding.UTF8);

		var blend = new BlendState
		{
			Color = new BlendComponent { SrcFactor = BlendFactor.SrcAlpha, DstFactor = BlendFactor.OneMinusSrcAlpha, Operation = BlendOperation.Add },
			Alpha = new BlendComponent { SrcFactor = BlendFactor.One, DstFactor = BlendFactor.OneMinusSrcAlpha, Operation = BlendOperation.Add },
		};

		SolidPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(CompareFunction.Always, StencilOperation.Keep), Face(CompareFunction.Always, StencilOperation.Keep), 0x00, 0x00);
		StencilEvenOdd = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(CompareFunction.Always, StencilOperation.Invert), Face(CompareFunction.Always, StencilOperation.Invert), 0xFF, 0xFF);
		StencilNonZero = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(CompareFunction.Always, StencilOperation.IncrementWrap), Face(CompareFunction.Always, StencilOperation.DecrementWrap), 0xFF, 0xFF);
		CoverPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(CompareFunction.NotEqual, StencilOperation.Zero), Face(CompareFunction.NotEqual, StencilOperation.Zero), 0xFF, 0xFF);
		CreateImagePipeline();
		CreateGradientPipeline(&blend);
	}

	// Evaluates a linear/radial gradient per pixel. The quad is positioned in NDC; the fragment uses its
	// framebuffer position (device pixels) so the gradient geometry can be baked to device space at record time.
	private const string GradientWgsl = @"
struct Grad { header: vec4<f32>, geo: vec4<f32>, colors: array<vec4<f32>, 16>, stops: array<vec4<f32>, 4> };
@group(0) @binding(0) var<uniform> g: Grad;
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return vec4<f32>(pos, 0.0, 1.0); }
fn stopAt(i: i32) -> f32 { return g.stops[i / 4][i % 4]; }
@fragment fn fs(@builtin(position) fc: vec4<f32>) -> @location(0) vec4<f32> {
  var t: f32 = 0.0;
  if (g.header.x < 0.5) {
    let a = g.geo.xy; let b = g.geo.zw; let ab = b - a; let denom = dot(ab, ab);
    if (denom > 0.0) { t = dot(fc.xy - a, ab) / denom; }
  } else {
    let c = g.geo.xy; let rx = g.geo.z;
    if (rx > 0.0) { t = distance(fc.xy, c) / rx; }
  }
  let tm = g.header.z;
  if (tm < 0.5) { t = clamp(t, 0.0, 1.0); }
  else if (tm < 1.5) { t = fract(t); }
  else { let f = fract(t * 0.5) * 2.0; if (f > 1.0) { t = 2.0 - f; } else { t = f; } }
  let n = i32(g.header.y);
  var col = g.colors[0];
  if (t <= stopAt(0)) { col = g.colors[0]; }
  else if (t >= stopAt(n - 1)) { col = g.colors[n - 1]; }
  else {
    for (var i = 0; i < n - 1; i = i + 1) {
      let s0 = stopAt(i); let s1 = stopAt(i + 1);
      if (t >= s0 && t <= s1) {
        var u = 0.0;
        if (s1 > s0) { u = (t - s0) / (s1 - s0); }
        col = mix(g.colors[i], g.colors[i + 1], u);
        break;
      }
    }
  }
  return col;
}";

	private void CreateGradientPipeline(BlendState* blend)
	{
		var module = Module(GradientWgsl);
		var vs = (byte*)SilkMarshal.StringToPtr("vs", NativeStringEncoding.UTF8);
		var fs = (byte*)SilkMarshal.StringToPtr("fs", NativeStringEncoding.UTF8);
		var attr = new VertexAttribute { Format = VertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		var vbl = new VertexBufferLayout { ArrayStride = 8, StepMode = VertexStepMode.Vertex, AttributeCount = 1, Attributes = &attr };
		var vsState = new VertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new ColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = ColorWriteMask.All };
		var fsState = new FragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var keepFace = Face(CompareFunction.Always, StencilOperation.Keep);
		var ds = new DepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = false, DepthCompare = CompareFunction.Always, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new RenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, StripIndexFormat = IndexFormat.Undefined, FrontFace = FrontFace.Ccw, CullMode = CullMode.None }, Multisample = new MultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = false }, Layout = null };
		GradientPipe = W.DeviceCreateRenderPipeline(Dev, ref pd);
		GradBgl = W.RenderPipelineGetBindGroupLayout(GradientPipe, 0);
	}

	private const string ImageWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
struct U { op: vec4<f32> };
@group(0) @binding(0) var tex: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: U;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) uv: vec2<f32>) -> VOut { var o: VOut; o.p = vec4<f32>(pos, 0.0, 1.0); o.uv = uv; return o; }
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return textureSample(tex, smp, i.uv) * u.op.x; }";

	private void CreateImagePipeline()
	{
		var module = Module(ImageWgsl);
		var vs = (byte*)SilkMarshal.StringToPtr("vs", NativeStringEncoding.UTF8);
		var fs = (byte*)SilkMarshal.StringToPtr("fs", NativeStringEncoding.UTF8);
		var attrs = stackalloc VertexAttribute[2];
		attrs[0] = new VertexAttribute { Format = VertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		attrs[1] = new VertexAttribute { Format = VertexFormat.Float32x2, Offset = 8, ShaderLocation = 1 };
		var vbl = new VertexBufferLayout { ArrayStride = 16, StepMode = VertexStepMode.Vertex, AttributeCount = 2, Attributes = attrs };
		var vsState = new VertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		// premultiplied image pixels -> One/OneMinusSrcAlpha
		var blend = new BlendState { Color = new BlendComponent { SrcFactor = BlendFactor.One, DstFactor = BlendFactor.OneMinusSrcAlpha, Operation = BlendOperation.Add }, Alpha = new BlendComponent { SrcFactor = BlendFactor.One, DstFactor = BlendFactor.OneMinusSrcAlpha, Operation = BlendOperation.Add } };
		var target = new ColorTargetState { Format = ColorFormat, Blend = &blend, WriteMask = ColorWriteMask.All };
		var fsState = new FragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var keepFace = Face(CompareFunction.Always, StencilOperation.Keep);
		var ds = new DepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = false, DepthCompare = CompareFunction.Always, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new RenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, StripIndexFormat = IndexFormat.Undefined, FrontFace = FrontFace.Ccw, CullMode = CullMode.None }, Multisample = new MultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = false }, Layout = null };
		ImagePipe = W.DeviceCreateRenderPipeline(Dev, ref pd);
		ImgBgl = W.RenderPipelineGetBindGroupLayout(ImagePipe, 0);
		var sd = new SamplerDescriptor { AddressModeU = AddressMode.ClampToEdge, AddressModeV = AddressMode.ClampToEdge, MagFilter = FilterMode.Linear, MinFilter = FilterMode.Linear, MipmapFilter = MipmapFilterMode.Nearest, MaxAnisotropy = 1 };
		Smp = W.DeviceCreateSampler(Dev, ref sd);
	}

	private RenderPipeline* MakePipe(ShaderModule* module, byte* vs, byte* fs, bool colorWrite, bool colorAttrs, BlendState* blend, StencilFaceState front, StencilFaceState back, uint stencilWrite, uint stencilRead)
	{
		var attrs = stackalloc VertexAttribute[2];
		attrs[0] = new VertexAttribute { Format = VertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		var stride = 8ul;
		var attrCount = 1u;
		if (colorAttrs)
		{
			attrs[1] = new VertexAttribute { Format = VertexFormat.Float32x4, Offset = 8, ShaderLocation = 1 };
			stride = 24; attrCount = 2;
		}
		var vbl = new VertexBufferLayout { ArrayStride = stride, StepMode = VertexStepMode.Vertex, AttributeCount = attrCount, Attributes = attrs };
		var vsState = new VertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new ColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = colorWrite ? ColorWriteMask.All : 0 };
		var fsState = new FragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var ds = new DepthStencilState
		{
			Format = DepthStencilFormat, DepthWriteEnabled = false, DepthCompare = CompareFunction.Always,
			StencilFront = front, StencilBack = back, StencilReadMask = stencilRead, StencilWriteMask = stencilWrite,
		};
		var pd = new RenderPipelineDescriptor
		{
			Vertex = vsState, Fragment = &fsState, DepthStencil = &ds,
			Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, StripIndexFormat = IndexFormat.Undefined, FrontFace = FrontFace.Ccw, CullMode = CullMode.None },
			Multisample = new MultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = false },
			Layout = null,
		};
		return W.DeviceCreateRenderPipeline(Dev, ref pd);
	}

	public void Dispose() { }
}

public sealed unsafe class WebGpuRenderSurface : IRenderTarget
{
	public Texture* Tex;
	public TextureView* View;              // single-sample resolve target (offscreen readback / swapchain image)
	public Texture* MsaaColorTex;
	public TextureView* MsaaColorView;     // multisampled color the pass renders into, resolved into View
	public Texture* DepthTex;
	public TextureView* DepthView;         // multisampled depth/stencil (clip mask + stencil-then-cover)
	public int Width { get; }
	public int Height { get; }
	public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
	public void Dispose() { }

	public WebGpuRenderSurface(WebGpuDevice device, int width, int height)
	{
		Width = width; Height = height;
		var td = new TextureDescriptor
		{
			Size = new Extent3D((uint)width, (uint)height, 1), Format = device.ColorFormat,
			MipLevelCount = 1, SampleCount = 1, Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment | TextureUsage.CopySrc,
		};
		Tex = device.W.DeviceCreateTexture(device.Dev, ref td);
		View = device.W.TextureCreateView(Tex, null);
		CreateMultisampledTargets(device, width, height);
	}

	// External-color variant for a swapchain: the color View/Tex are provided per frame (the acquired
	// swapchain image, used as the resolve target); the multisampled color + depth are owned here.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, bool externalColor)
	{
		Width = width; Height = height;
		CreateMultisampledTargets(device, width, height);
	}

	private void CreateMultisampledTargets(WebGpuDevice device, int width, int height)
	{
		var cd = new TextureDescriptor
		{
			Size = new Extent3D((uint)width, (uint)height, 1), Format = device.ColorFormat,
			MipLevelCount = 1, SampleCount = WebGpuDevice.MsaaSamples, Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment,
		};
		MsaaColorTex = device.W.DeviceCreateTexture(device.Dev, ref cd);
		MsaaColorView = device.W.TextureCreateView(MsaaColorTex, null);

		var dd = new TextureDescriptor
		{
			Size = new Extent3D((uint)width, (uint)height, 1), Format = WebGpuDevice.DepthStencilFormat,
			MipLevelCount = 1, SampleCount = WebGpuDevice.MsaaSamples, Dimension = TextureDimension.Dimension2D, Usage = TextureUsage.RenderAttachment,
		};
		DepthTex = device.W.DeviceCreateTexture(device.Dev, ref dd);
		DepthView = device.W.TextureCreateView(DepthTex, null);
	}
}

// Draw commands share one ordered stream so cross-type z-order (rect over path over image) is preserved.
internal abstract class WebGpuCommand
{
	public Vector4 Clip;
}

internal sealed class RectCommand : WebGpuCommand
{
	public WColor Color;
	public Vector2 P0, P1, P2, P3;
}

internal sealed class PathFill : WebGpuCommand
{
	public float[] FanDevice;
	public Vector2 BbMin, BbMax;
	public WColor Color;
	public bool EvenOdd;
}

internal sealed unsafe class ImageCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;
	public TextureView* View;   // the pre-uploaded WebGpuImageTexture view (no per-frame upload)
	public int W, H;
	public float Opacity;
	public float U0, V0, U1 = 1f, V1 = 1f;   // source UV sub-rect (whole texture by default)
}

internal sealed class GradientCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;   // device-space quad
	public float[] Uniform;          // packed Grad struct (WebGpuDevice.GradientUniformBytes / 4 floats)
}

// Backend-created gradient shader handle. The WebGPU backend mints its own (rather than delegating to Skia) so
// the recorder can read the gradient parameters back and evaluate them in the WGSL gradient pipeline.
public sealed class WebGpuShader : IShader
{
	public bool Radial;
	public Vector2 P0;          // start (linear) / center (radial), in gradient-local space
	public Vector2 P1;          // end (linear) / gradient origin (radial)
	public float RadiusX, RadiusY;
	public WColor[] Colors;
	public float[] Stops;
	public GradientTileMode TileMode;
	public Matrix3x2 LocalMatrix;
}

public sealed class WebGpuRenderData : IRenderData
{
	internal List<WebGpuCommand> Commands = new();
	internal WColor? ClearColor;
	public void Dispose() { Commands = null; }
}

public sealed unsafe class WebGpuCommandRecorder : ICommandRecorder, IFlattenedPathSink
{
	private readonly Stack<(Matrix4x4 m, Vector4 clip)> _stack = new();
	private Matrix4x4 _m = Matrix4x4.Identity;
	private Vector4 _clip = new(-1e9f, -1e9f, 1e9f, 1e9f); // device-space L,T,R,B
	private readonly WebGpuRenderData _data = new();

	public Matrix4x4 TotalMatrix => _m;
	public void SetMatrix(in Matrix4x4 matrix) => _m = matrix;
	public void Concat(in Matrix4x4 matrix) => _m = matrix * _m;
	public void Translate(float dx, float dy) => _m = Matrix4x4.CreateTranslation(dx, dy, 0) * _m;
	public void Scale(float sx, float sy) => _m = Matrix4x4.CreateScale(sx, sy, 1) * _m;
	public int Save() { _stack.Push((_m, _clip)); return _stack.Count; }
	public int SaveCount => _stack.Count;
	public void Restore() { if (_stack.Count > 0) { var t = _stack.Pop(); _m = t.m; _clip = t.clip; } }
	public void RestoreToCount(int count) { while (_stack.Count > count) { var t = _stack.Pop(); _m = t.m; _clip = t.clip; } }
	public void SaveLayer(bool antialias = false) => Save();
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false) => Save();
	public void SaveLayer(BlendMode blendMode, bool antialias = false) => Save();
	public void SaveLayer(IEffectFilter filter) => Save();
	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		// Axis-aligned device AABB of the clip rect, intersected with the current clip (Intersect only; scissor).
		var a = Map((float)rect.Left, (float)rect.Top); var b = Map((float)rect.Right, (float)rect.Top);
		var c = Map((float)rect.Right, (float)rect.Bottom); var d = Map((float)rect.Left, (float)rect.Bottom);
		var l = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(c.X, d.X)); var t = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(c.Y, d.Y));
		var r = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(c.X, d.X)); var bo = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(c.Y, d.Y));
		_clip = new Vector4(MathF.Max(_clip.X, l), MathF.Max(_clip.Y, t), MathF.Min(_clip.Z, r), MathF.Min(_clip.W, bo));
	}
	// Round-rect / path clips approximated by their AABB scissor for now (corners not yet masked).
	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) => ClipRect(roundRect.Rect, operation, antialias);
	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { var b = geometry.Bounds; ClipRect(b, operation, antialias); }
	public void Clear(WColor color) => _data.ClearColor = color;

	private Vector2 Map(float x, float y) => new(x * _m.M11 + y * _m.M21 + _m.M41, x * _m.M12 + y * _m.M22 + _m.M42);

	public void DrawRect(in Rect rect, WColor color, bool antialias = false)
		=> _data.Commands.Add(new RectCommand
		{
			Color = color, Clip = _clip,
			P0 = Map((float)rect.Left, (float)rect.Top), P1 = Map((float)rect.Right, (float)rect.Top),
			P2 = Map((float)rect.Right, (float)rect.Bottom), P3 = Map((float)rect.Left, (float)rect.Bottom),
		});

	private List<float> _fan;
	private Vector2 _pivot, _prev, _bbMin, _bbMax;
	private bool _firstInContour;

	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false)
		=> FillGeometry(geometry, color, geometry.FillRule == GeometryFillRule.EvenOdd);

	private void FillGeometry(IGeometry geometry, WColor color, bool evenOdd)
	{
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		geometry.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_data.Commands.Add(new PathFill { FanDevice = _fan.ToArray(), BbMin = _bbMin, BbMax = _bbMax, Color = color, EvenOdd = evenOdd, Clip = _clip });
		}
		_fan = null;
	}

	void IFlattenedPathSink.BeginContour(Vector2 start) { _pivot = Map(start.X, start.Y); _prev = _pivot; _firstInContour = true; Include(_pivot); }
	void IFlattenedPathSink.LineTo(Vector2 point)
	{
		var p = Map(point.X, point.Y); Include(p);
		if (_firstInContour) { _firstInContour = false; }
		else { _fan.Add(_pivot.X); _fan.Add(_pivot.Y); _fan.Add(_prev.X); _fan.Add(_prev.Y); _fan.Add(p.X); _fan.Add(p.Y); }
		_prev = p;
	}
	void IFlattenedPathSink.EndContour(bool closed) { }
	private void Include(Vector2 p) { _bbMin = Vector2.Min(_bbMin, p); _bbMax = Vector2.Max(_bbMax, p); }

	public void DrawRect(in Rect rect, IShader shader, bool antialias = false)
	{
		if (shader is not WebGpuShader g)
		{
			return;
		}

		// Compose the gradient's local matrix with the current matrix, so gradient geometry is baked to device
		// space (the WGSL fragment evaluates in device pixels). Linear is exact under affine; radial radii are
		// scaled by the combined axis lengths (an ellipse under non-uniform scale is approximated).
		var lm = new Matrix4x4(
			g.LocalMatrix.M11, g.LocalMatrix.M12, 0, 0,
			g.LocalMatrix.M21, g.LocalMatrix.M22, 0, 0,
			0, 0, 1, 0,
			g.LocalMatrix.M31, g.LocalMatrix.M32, 0, 1);
		var m = lm * _m;
		Vector2 MapM(Vector2 p) => new(p.X * m.M11 + p.Y * m.M21 + m.M41, p.X * m.M12 + p.Y * m.M22 + m.M42);
		var a = MapM(g.P0);
		var b = MapM(g.P1);
		var sx = new Vector2(m.M11, m.M12).Length();
		var sy = new Vector2(m.M21, m.M22).Length();

		var count = Math.Min(g.Colors?.Length ?? 0, WebGpuDevice.MaxGradientStops);
		if (count == 0)
		{
			return;
		}

		var u = new float[WebGpuDevice.GradientUniformBytes / 4];
		u[0] = g.Radial ? 1f : 0f;
		u[1] = count;
		u[2] = g.TileMode switch { GradientTileMode.Repeat => 1f, GradientTileMode.Mirror => 2f, _ => 0f };
		if (g.Radial)
		{
			u[4] = a.X; u[5] = a.Y; u[6] = g.RadiusX * sx; u[7] = g.RadiusY * sy;
		}
		else
		{
			u[4] = a.X; u[5] = a.Y; u[6] = b.X; u[7] = b.Y;
		}

		for (var i = 0; i < count; i++)
		{
			var c = g.Colors[i];
			u[8 + i * 4] = c.R / 255f;
			u[8 + i * 4 + 1] = c.G / 255f;
			u[8 + i * 4 + 2] = c.B / 255f;
			u[8 + i * 4 + 3] = c.A / 255f;
			u[72 + i] = g.Stops is { Length: > 0 } && i < g.Stops.Length ? g.Stops[i] : (count > 1 ? i / (float)(count - 1) : 0f);
		}

		_data.Commands.Add(new GradientCmd
		{
			Clip = _clip, Uniform = u,
			P0 = Map((float)rect.Left, (float)rect.Top), P1 = Map((float)rect.Right, (float)rect.Top),
			P2 = Map((float)rect.Right, (float)rect.Bottom), P3 = Map((float)rect.Left, (float)rect.Bottom),
		});
	}
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false) { }
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false)
	{
		using var sg = geometry.GetStrokeFillGeometry(new StrokeStyle { Thickness = strokeWidth, LineJoin = StrokeJoin.Miter, MiterLimit = 10f });
		FillGeometry(sg, color, evenOdd: false);
	}
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false)
	{
		var dir = p1 - p0; var len = dir.Length(); if (len < 1e-4f) { return; } dir /= len;
		var n = new Vector2(-dir.Y, dir.X) * (strokeWidth / 2f);
		_data.Commands.Add(new RectCommand
		{
			Color = color, Clip = _clip,
			P0 = Map(p0.X + n.X, p0.Y + n.Y), P1 = Map(p1.X + n.X, p1.Y + n.Y),
			P2 = Map(p1.X - n.X, p1.Y - n.Y), P3 = Map(p0.X - n.X, p0.Y - n.Y),
		});
	}
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false)
	{
		if (texture is not WebGpuImageTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		// No per-frame upload — the texture is already resident; record its view for the present pass.
		_data.Commands.Add(new ImageCmd { P0 = Map(x, y), P1 = Map(x + w, y), P2 = Map(x + w, y + h), P3 = Map(x, y + h), View = t.View, W = w, H = h, Opacity = opacity, Clip = _clip });
	}
	// Color-filtered (tinted) image draw isn't supported yet on WebGPU — fall back to an untinted draw.
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) => DrawImage(texture, x, y, sampling, 1f, antialias);

	public void DrawImageNineSlice(IImageTexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false)
	{
		if (texture is not WebGpuImageTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }

		// Source (pixel) column/row edges from the center slice, and the matching destination edges: the corner
		// insets keep their source pixel size, the middle band stretches to fill the rest of the destination.
		float sx0 = 0, sx1 = (float)centerSlice.Left, sx2 = (float)centerSlice.Right, sx3 = w;
		float sy0 = 0, sy1 = (float)centerSlice.Top, sy2 = (float)centerSlice.Bottom, sy3 = h;
		float dx0 = (float)destination.Left, dx1 = dx0 + sx1, dx3 = (float)destination.Right, dx2 = dx3 - (sx3 - sx2);
		float dy0 = (float)destination.Top, dy1 = dy0 + sy1, dy3 = (float)destination.Bottom, dy2 = dy3 - (sy3 - sy2);
		float[] sxe = { sx0, sx1, sx2, sx3 }, sye = { sy0, sy1, sy2, sy3 };
		float[] dxe = { dx0, dx1, dx2, dx3 }, dye = { dy0, dy1, dy2, dy3 };

		for (var row = 0; row < 3; row++)
		{
			for (var col = 0; col < 3; col++)
			{
				if (centerHollow && row == 1 && col == 1) { continue; }
				float dl = dxe[col], dr = dxe[col + 1], dt = dye[row], db = dye[row + 1];
				if (dr - dl <= 0 || db - dt <= 0) { continue; }
				_data.Commands.Add(new ImageCmd
				{
					View = t.View, W = w, H = h, Opacity = 1f, Clip = _clip,
					P0 = Map(dl, dt), P1 = Map(dr, dt), P2 = Map(dr, db), P3 = Map(dl, db),
					U0 = sxe[col] / w, V0 = sye[row] / h, U1 = sxe[col + 1] / w, V1 = sye[row + 1] / h,
				});
			}
		}
	}
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) { }

	public IRenderData Finish() => _data;
	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();

	// Retained sub-recordings (SKPicture equivalent) are recorded at identity; replaying one bakes in
	// the target session's current matrix + clip — matching Skia's sk_canvas_draw_picture semantics.
	public void Replay(IRenderData data)
	{
		if (data is not WebGpuRenderData d) { return; }
		Vector2 T(Vector2 p) => new(p.X * _m.M11 + p.Y * _m.M21 + _m.M41, p.X * _m.M12 + p.Y * _m.M22 + _m.M42);
		foreach (var cmd in d.Commands)
		{
			switch (cmd)
			{
				case RectCommand r:
					_data.Commands.Add(new RectCommand { Color = r.Color, Clip = ClipCompose(r.Clip, T), P0 = T(r.P0), P1 = T(r.P1), P2 = T(r.P2), P3 = T(r.P3) });
					break;
				case PathFill p:
					var src = p.FanDevice; var dst = new float[src.Length];
					var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
					for (int i = 0; i < src.Length; i += 2)
					{
						var q = T(new Vector2(src[i], src[i + 1])); dst[i] = q.X; dst[i + 1] = q.Y;
						bbMin = Vector2.Min(bbMin, q); bbMax = Vector2.Max(bbMax, q);
					}
					_data.Commands.Add(new PathFill { FanDevice = dst, BbMin = bbMin, BbMax = bbMax, Color = p.Color, EvenOdd = p.EvenOdd, Clip = ClipCompose(p.Clip, T) });
					break;
				case ImageCmd im:
					_data.Commands.Add(new ImageCmd { P0 = T(im.P0), P1 = T(im.P1), P2 = T(im.P2), P3 = T(im.P3), View = im.View, W = im.W, H = im.H, Opacity = im.Opacity, U0 = im.U0, V0 = im.V0, U1 = im.U1, V1 = im.V1, Clip = ClipCompose(im.Clip, T) });
					break;
				case GradientCmd gc:
					// Transform the device-space geometry baked into the uniform by the replay matrix too, so the
					// gradient stays aligned with its (transformed) quad.
					var uu = (float[])gc.Uniform.Clone();
					var ga = T(new Vector2(uu[4], uu[5])); uu[4] = ga.X; uu[5] = ga.Y;
					if (uu[0] < 0.5f)
					{
						var gb = T(new Vector2(uu[6], uu[7])); uu[6] = gb.X; uu[7] = gb.Y;
					}
					else
					{
						var s = new Vector2(_m.M11, _m.M12).Length();
						uu[6] *= s; uu[7] *= s;
					}
					_data.Commands.Add(new GradientCmd { P0 = T(gc.P0), P1 = T(gc.P1), P2 = T(gc.P2), P3 = T(gc.P3), Uniform = uu, Clip = ClipCompose(gc.Clip, T) });
					break;
			}
		}
	}

	// Transform a child clip AABB by the current matrix and intersect it with the current clip.
	private Vector4 ClipCompose(Vector4 c, Func<Vector2, Vector2> t)
	{
		if (c.X <= -1e8f && c.Y <= -1e8f && c.Z >= 1e8f && c.W >= 1e8f) { return _clip; }
		var a = t(new Vector2(c.X, c.Y)); var b = t(new Vector2(c.Z, c.Y)); var e = t(new Vector2(c.Z, c.W)); var f = t(new Vector2(c.X, c.W));
		var l = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(e.X, f.X)); var top = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(e.Y, f.Y));
		var r = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(e.X, f.X)); var bo = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(e.Y, f.Y));
		return new Vector4(MathF.Max(_clip.X, l), MathF.Max(_clip.Y, top), MathF.Min(_clip.Z, r), MathF.Min(_clip.W, bo));
	}
}

public sealed unsafe class WebGpuPresentSession : IPresentSession
{
	private readonly WebGpuDevice _d;
	private readonly WebGpuRenderSurface _s;
	private WColor? _presentClear;
	public WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s) { _d = d; _s = s; }

	private bool SetScissor(RenderPassEncoder* pass, Vector4 clip)
	{
		int x = (int)MathF.Max(0, MathF.Floor(clip.X)); int y = (int)MathF.Max(0, MathF.Floor(clip.Y));
		int r = (int)MathF.Min(_s.Width, MathF.Ceiling(clip.Z)); int b = (int)MathF.Min(_s.Height, MathF.Ceiling(clip.W));
		int w = r - x, h = b - y; if (w <= 0 || h <= 0) { return false; }
		_d.W.RenderPassEncoderSetScissorRect(pass, (uint)x, (uint)y, (uint)w, (uint)h); return true;
	}
	private Vector2 Ndc(Vector2 dev) => new(2f * dev.X / _s.Width - 1f, 1f - 2f * dev.Y / _s.Height);

	private Silk.NET.WebGPU.Buffer* MakeBuffer(float[] data)
	{
		var size = (nuint)(data.Length * sizeof(float));
		var bd = new BufferDescriptor { Size = size, Usage = BufferUsage.Vertex | BufferUsage.CopyDst };
		var buf = _d.W.DeviceCreateBuffer(_d.Dev, ref bd);
		fixed (float* p = data) { _d.W.QueueWriteBuffer(_d.Q, buf, 0, p, size); }
		return buf;
	}

	public void Replay(IRenderData data)
	{
		var rd = (WebGpuRenderData)data;
		var W = _d.W;

		// Build GPU resources for every command up front (buffers/textures must be created outside the
		// render pass), preserving draw order in a single op list so cross-type z-order is honoured.
		// kind: 0=rect (b0=verts), 1=path (b0=fan, u0=fanCount, b1=cover, flag=evenOdd), 2=image (b0=bindGroup, b1=quad).
		var ops = new List<(int kind, nint b0, uint u0, nint b1, bool flag, Vector4 clip)>();
		foreach (var cmd in rd.Commands)
		{
			switch (cmd)
			{
				case RectCommand rc:
				{
					var c = new Vector4(rc.Color.R / 255f, rc.Color.G / 255f, rc.Color.B / 255f, rc.Color.A / 255f);
					var v = new List<float>();
					void V(Vector2 p) { var n = Ndc(p); v.Add(n.X); v.Add(n.Y); v.Add(c.X); v.Add(c.Y); v.Add(c.Z); v.Add(c.W); }
					V(rc.P0); V(rc.P1); V(rc.P2); V(rc.P0); V(rc.P2); V(rc.P3);
					ops.Add((0, (nint)MakeBuffer(v.ToArray()), 0, 0, false, rc.Clip));
					break;
				}
				case PathFill pf:
				{
					var fanNdc = new float[pf.FanDevice.Length];
					for (int i = 0; i < pf.FanDevice.Length; i += 2) { var n = Ndc(new Vector2(pf.FanDevice[i], pf.FanDevice[i + 1])); fanNdc[i] = n.X; fanNdc[i + 1] = n.Y; }
					var fanBuf = MakeBuffer(fanNdc);
					var c = new Vector4(pf.Color.R / 255f, pf.Color.G / 255f, pf.Color.B / 255f, pf.Color.A / 255f);
					var cov = new List<float>();
					void CV(Vector2 p) { var n = Ndc(p); cov.Add(n.X); cov.Add(n.Y); cov.Add(c.X); cov.Add(c.Y); cov.Add(c.Z); cov.Add(c.W); }
					var tl = pf.BbMin; var br = pf.BbMax; var tr = new Vector2(br.X, tl.Y); var bl = new Vector2(tl.X, br.Y);
					CV(tl); CV(tr); CV(br); CV(tl); CV(br); CV(bl);
					ops.Add((1, (nint)fanBuf, (uint)(pf.FanDevice.Length / 2), (nint)MakeBuffer(cov.ToArray()), pf.EvenOdd, pf.Clip));
					break;
				}
				case ImageCmd im:
				{
					// The texture is already resident (WebGpuImageTexture); just bind its view — no upload.
					var view = im.View;
					var ubd = new BufferDescriptor { Size = 16, Usage = BufferUsage.Uniform | BufferUsage.CopyDst };
					var ubuf = W.DeviceCreateBuffer(_d.Dev, ref ubd);
					var op = stackalloc float[4]; op[0] = im.Opacity; op[1] = op[2] = op[3] = 0;
					W.QueueWriteBuffer(_d.Q, ubuf, 0, op, 16);
					var entries = stackalloc BindGroupEntry[3];
					entries[0] = new BindGroupEntry { Binding = 0, TextureView = view };
					entries[1] = new BindGroupEntry { Binding = 1, Sampler = _d.Smp };
					entries[2] = new BindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 16 };
					var bgd = new BindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
					var bg = W.DeviceCreateBindGroup(_d.Dev, ref bgd);
					var q = new float[24];
					void QV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); q[idx] = n.X; q[idx + 1] = n.Y; q[idx + 2] = u; q[idx + 3] = vv; }
					QV(0, im.P0, im.U0, im.V0); QV(4, im.P1, im.U1, im.V0); QV(8, im.P2, im.U1, im.V1); QV(12, im.P0, im.U0, im.V0); QV(16, im.P2, im.U1, im.V1); QV(20, im.P3, im.U0, im.V1);
					ops.Add((2, (nint)bg, 0, (nint)MakeBuffer(q), false, im.Clip));
					break;
				}
				case GradientCmd gc:
				{
					var bytes = (nuint)WebGpuDevice.GradientUniformBytes;
					var ubd = new BufferDescriptor { Size = bytes, Usage = BufferUsage.Uniform | BufferUsage.CopyDst };
					var ubuf = W.DeviceCreateBuffer(_d.Dev, ref ubd);
					fixed (float* p = gc.Uniform) { W.QueueWriteBuffer(_d.Q, ubuf, 0, p, bytes); }
					var gentry = new BindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = bytes };
					var gbgd = new BindGroupDescriptor { Layout = _d.GradBgl, EntryCount = 1, Entries = &gentry };
					var gbg = W.DeviceCreateBindGroup(_d.Dev, ref gbgd);
					var gq = new float[12];
					void GV(int idx, Vector2 pos) { var n = Ndc(pos); gq[idx] = n.X; gq[idx + 1] = n.Y; }
					GV(0, gc.P0); GV(2, gc.P1); GV(4, gc.P2); GV(6, gc.P0); GV(8, gc.P2); GV(10, gc.P3);
					ops.Add((3, (nint)gbg, 0, (nint)MakeBuffer(gq), false, gc.Clip));
					break;
				}
			}
		}

		var enc = W.DeviceCreateCommandEncoder(_d.Dev, null);
		// The neutral loop calls present.Clear(...) before Replay; honor it (else fall back to the frame's clear).
		var clear = _presentClear ?? rd.ClearColor;
		var ca = new RenderPassColorAttachment
		{
			// Render into the multisampled color and resolve into the single-sample present/readback texture.
			// A fresh MSAA buffer can't LoadOp.Load, so we always clear (transparent when no clear was given);
			// the neutral loop redraws the whole frame each present, so nothing prior needs preserving here.
			View = _s.MsaaColorView, ResolveTarget = _s.View, LoadOp = LoadOp.Clear, StoreOp = StoreOp.Discard,
			ClearValue = clear.HasValue ? new Silk.NET.WebGPU.Color(clear.Value.R / 255.0, clear.Value.G / 255.0, clear.Value.B / 255.0, clear.Value.A / 255.0) : default,
		};
		var dsa = new RenderPassDepthStencilAttachment
		{
			View = _s.DepthView,
			DepthLoadOp = LoadOp.Clear, DepthStoreOp = StoreOp.Store, DepthClearValue = 1f,
			StencilLoadOp = LoadOp.Clear, StencilStoreOp = StoreOp.Store, StencilClearValue = 0,
		};
		var rp = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
		var pass = W.CommandEncoderBeginRenderPass(enc, ref rp);

		foreach (var (kind, b0, u0, b1, flag, clip) in ops)
		{
			if (!SetScissor(pass, clip)) { continue; }
			switch (kind)
			{
				case 0:
					W.RenderPassEncoderSetPipeline(pass, _d.SolidPipe);
					W.RenderPassEncoderSetVertexBuffer(pass, 0, (Silk.NET.WebGPU.Buffer*)b0, 0, (nuint)(36 * sizeof(float)));
					W.RenderPassEncoderDraw(pass, 6, 1, 0, 0);
					break;
				case 1:
					W.RenderPassEncoderSetPipeline(pass, flag ? _d.StencilEvenOdd : _d.StencilNonZero);
					W.RenderPassEncoderSetVertexBuffer(pass, 0, (Silk.NET.WebGPU.Buffer*)b0, 0, (nuint)(u0 * 2 * sizeof(float)));
					W.RenderPassEncoderDraw(pass, u0, 1, 0, 0);
					W.RenderPassEncoderSetPipeline(pass, _d.CoverPipe);
					W.RenderPassEncoderSetStencilReference(pass, 0);
					W.RenderPassEncoderSetVertexBuffer(pass, 0, (Silk.NET.WebGPU.Buffer*)b1, 0, (nuint)(36 * sizeof(float)));
					W.RenderPassEncoderDraw(pass, 6, 1, 0, 0);
					break;
				case 2:
					W.RenderPassEncoderSetPipeline(pass, _d.ImagePipe);
					W.RenderPassEncoderSetBindGroup(pass, 0, (BindGroup*)b0, 0, (uint*)null);
					W.RenderPassEncoderSetVertexBuffer(pass, 0, (Silk.NET.WebGPU.Buffer*)b1, 0, (nuint)(24 * sizeof(float)));
					W.RenderPassEncoderDraw(pass, 6, 1, 0, 0);
					break;
				case 3:
					W.RenderPassEncoderSetPipeline(pass, _d.GradientPipe);
					W.RenderPassEncoderSetBindGroup(pass, 0, (BindGroup*)b0, 0, (uint*)null);
					W.RenderPassEncoderSetVertexBuffer(pass, 0, (Silk.NET.WebGPU.Buffer*)b1, 0, (nuint)(12 * sizeof(float)));
					W.RenderPassEncoderDraw(pass, 6, 1, 0, 0);
					break;
			}
		}

		W.RenderPassEncoderEnd(pass);
		var cb = W.CommandEncoderFinish(enc, null);
		W.QueueSubmit(_d.Q, 1, &cb);
		_d.Native.DevicePoll(_d.Dev, true, null);
	}

	public Matrix4x4 TotalMatrix => Matrix4x4.Identity;
	public void SetMatrix(in Matrix4x4 matrix) { }
	public void Concat(in Matrix4x4 matrix) { }
	public void Translate(float dx, float dy) { }
	public void Scale(float sx, float sy) { }
	public int Save() => 0;
	public int SaveCount => 0;
	public void Restore() { }
	public void RestoreToCount(int count) { }
	public void SaveLayer(bool antialias = false) { }
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false) { }
	public void SaveLayer(BlendMode blendMode, bool antialias = false) { }
	public void SaveLayer(IEffectFilter filter) { }
	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void Clear(WColor color) => _presentClear = color;
	public void DrawRect(in Rect rect, WColor color, bool antialias = false) { }
	public void DrawRect(in Rect rect, IShader shader, bool antialias = false) { }
	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false) { }
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false) { }
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false) { }
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) { }
	public void DrawImageNineSlice(IImageTexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false) { }
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) { }
	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();
	public void Dispose() { }
}

public sealed class WebGpuRenderer : IRenderer
{
	public readonly WebGpuDevice Device;
	public WebGpuRenderer(WebGpuDevice device) => Device = device;
	public ICommandRecorder BeginFrame() => new WebGpuCommandRecorder();
	public IPresentSession BeginPresent(IRenderTarget target) => new WebGpuPresentSession(Device, (WebGpuRenderSurface)target);
}

// --- New-SPI pluggable-backend surface (see doc/uno-drawing-backend-abstraction.md) ---

/// <summary>A <see cref="IGraphicsContext"/> wrapping a <see cref="WebGpuDevice"/>. Created by the graphics-layer context factory for <see cref="GraphicsContextKind.WebGpu"/>.</summary>
public sealed class WebGpuGraphicsContext : IGraphicsContext
{
	public WebGpuGraphicsContext(WebGpuDevice device) => Device = device;

	public WebGpuDevice Device { get; }

	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	public bool IsLost => false;

	// The WebGPU render path is driven by the (legacy, env-gated) X11WebGpu*Renderer today, not GraphicsRegistry
	// .Activate, so this context isn't asked to acquire/present. Stubbed until the WebGPU host adopts the seam.
	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> throw new System.NotSupportedException("WebGpuGraphicsContext does not yet drive AcquireRenderTarget/Present.");

	public void Present()
		=> throw new System.NotSupportedException("WebGpuGraphicsContext does not yet drive AcquireRenderTarget/Present.");

	public void Dispose() { }
}

/// <summary>The registerable WebGPU backend pair. Prefers a WebGPU context; needs an 8-bit stencil for path fills.</summary>
public sealed class WebGpuGraphicsProvider : IGraphicsProvider
{
	private static readonly GraphicsContextKind[] _preferred = { GraphicsContextKind.WebGpu };

	private readonly IDrawingFactory _drawing;

	public WebGpuGraphicsProvider(IDrawingFactory drawing) => _drawing = drawing;

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	public GraphicsRequirements Requirements => new() { MinStencilBits = 8, PreferredColor = GraphicsColorFormat.Rgba8888 };

	// Geometry/images are neutral, so the WebGPU backend can reuse a shared managed factory. Shaders/filters/
	// effects would need a WebGPU-owned factory — deferred; this proves the negotiation + render path.
	public Uno.UI.Composition.Drawing.Graphics CreateGraphics(IGraphicsContext context) => new(_drawing, new WebGpuRenderer(((WebGpuGraphicsContext)context).Device));
}

// --- Device-bound factory (IImageTexture + eventual shaders) ---

/// <summary>A wgpu texture uploaded once from a neutral <see cref="IImage"/>'s pixels. Owned/disposed by the framework.</summary>
public sealed unsafe class WebGpuImageTexture : IImageTexture
{
	private readonly WebGpuDevice _d;
	private readonly IImage _source; // kept for the neutral CopyPixels cross-backend fallback
	public Texture* Tex;
	public TextureView* View;

	public int PixelWidth { get; }
	public int PixelHeight { get; }

	public void CopyPixels(Span<byte> destination) => _source.CopyPixels(destination);

	public WebGpuImageTexture(WebGpuDevice device, IImage image)
	{
		_d = device;
		_source = image;
		int w = image.PixelWidth, h = image.PixelHeight;
		PixelWidth = w; PixelHeight = h;
		var bgra = new byte[w * h * 4];
		image.CopyPixels(bgra);
		var rgba = new byte[w * h * 4];
		for (int i = 0; i < bgra.Length; i += 4) { rgba[i] = bgra[i + 2]; rgba[i + 1] = bgra[i + 1]; rgba[i + 2] = bgra[i]; rgba[i + 3] = bgra[i + 3]; }
		var td = new TextureDescriptor { Size = new Extent3D((uint)w, (uint)h, 1), Format = TextureFormat.Rgba8Unorm, MipLevelCount = 1, SampleCount = 1, Dimension = TextureDimension.Dimension2D, Usage = TextureUsage.TextureBinding | TextureUsage.CopyDst | TextureUsage.CopySrc };
		Tex = device.W.DeviceCreateTexture(device.Dev, ref td);
		View = device.W.TextureCreateView(Tex, null);
		var dst = new ImageCopyTexture { Texture = Tex, Aspect = TextureAspect.All, MipLevel = 0, Origin = default };
		var layout = new TextureDataLayout { BytesPerRow = (uint)(w * 4), RowsPerImage = (uint)h };
		var ext = new Extent3D((uint)w, (uint)h, 1);
		fixed (byte* p = rgba) { device.W.QueueWriteTexture(device.Q, in dst, p, (nuint)rgba.Length, in layout, in ext); }
	}

	public void Dispose()
	{
		if (View != null) { _d.W.TextureViewRelease(View); View = null; }
		if (Tex != null) { _d.W.TextureDestroy(Tex); Tex = null; }
	}
}

/// <summary>
/// The device-bound WebGPU resource factory. It WRAPS an inner factory (the host's existing one — Skia or
/// managed), overriding only <see cref="CreateImageTexture"/> to produce a wgpu texture on the device.
/// Everything else (geometry, decode, image frames, shaders, filters, offscreen) delegates to the inner
/// factory — so a real app renders unchanged, but images become GPU-resident wgpu textures the WebGPU
/// renderer consumes. (A future WebGpuShader would move shader creation here too.)
/// </summary>
public sealed class WebGpuDrawingFactory : IDrawingFactory
{
	private readonly WebGpuDevice _device;
	private readonly IDrawingFactory _inner;

	public WebGpuDrawingFactory(WebGpuDevice device, IDrawingFactory inner) { _device = device; _inner = inner; }

	public IImageTexture CreateImageTexture(IImage image) => new WebGpuImageTexture(_device, image);

	public IPathBuilder CreatePathBuilder() => _inner.CreatePathBuilder();
	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => _inner.CreatePrimitiveGeometryBuilder();
	public IGeometry CreateRectangleGeometry(Windows.Foundation.Rect rect) => _inner.CreateRectangleGeometry(rect);
	public IImage RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render) => _inner.RenderOffscreen(pixelWidth, pixelHeight, render);
	public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = false, P0 = start, P1 = end, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = true, P0 = center, P1 = gradientOrigin, RadiusX = radiusX, RadiusY = radiusY, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IColorFilter CreateBlendModeColorFilter(WColor color, BlendMode mode) => _inner.CreateBlendModeColorFilter(color, mode);
	public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => _inner.CreateColorMatrixColorFilter(matrix);
	public IEffectFilter CreateEffectFilter(Windows.Graphics.Effects.IGraphicsEffect effect, Windows.Foundation.Rect bounds, Func<string, Microsoft.UI.Composition.CompositionBrush> sourceResolver, bool useBackdropBlurClamp, bool isSoftwareRenderer, out bool hasBackdropInput) => _inner.CreateEffectFilter(effect, bounds, sourceResolver, useBackdropBlurClamp, isSoftwareRenderer, out hasBackdropInput);
	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, WColor color) => _inner.CreateDropShadowFilter(dx, dy, sigmaX, sigmaY, color);
}
