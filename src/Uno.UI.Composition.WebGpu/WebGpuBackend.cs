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
	public RenderPipeline* StencilPipe;
	public RenderPipeline* CoverPipe;

	public const TextureFormat ColorFormat = TextureFormat.Rgba8Unorm;
	public const TextureFormat DepthStencilFormat = TextureFormat.Depth24PlusStencil8;

	public WebGpuDevice()
	{
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

		SolidPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(CompareFunction.Always, StencilOperation.Keep), 0x00, 0x00);
		StencilPipe = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(CompareFunction.Always, StencilOperation.Invert), 0xFF, 0xFF);
		CoverPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(CompareFunction.NotEqual, StencilOperation.Zero), 0xFF, 0xFF);
	}

	private RenderPipeline* MakePipe(ShaderModule* module, byte* vs, byte* fs, bool colorWrite, bool colorAttrs, BlendState* blend, StencilFaceState face, uint stencilWrite, uint stencilRead)
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
			StencilFront = face, StencilBack = face, StencilReadMask = stencilRead, StencilWriteMask = stencilWrite,
		};
		var pd = new RenderPipelineDescriptor
		{
			Vertex = vsState, Fragment = &fsState, DepthStencil = &ds,
			Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, StripIndexFormat = IndexFormat.Undefined, FrontFace = FrontFace.Ccw, CullMode = CullMode.None },
			Multisample = new MultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = false },
			Layout = null,
		};
		return W.DeviceCreateRenderPipeline(Dev, ref pd);
	}

	public void Dispose() { }
}

public sealed unsafe class WebGpuRenderSurface : IRenderSurface
{
	public Texture* Tex;
	public TextureView* View;
	public Texture* DepthTex;
	public TextureView* DepthView;
	public readonly int Width, Height;

	public WebGpuRenderSurface(WebGpuDevice device, int width, int height)
	{
		Width = width; Height = height;
		var td = new TextureDescriptor
		{
			Size = new Extent3D((uint)width, (uint)height, 1), Format = WebGpuDevice.ColorFormat,
			MipLevelCount = 1, SampleCount = 1, Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment | TextureUsage.CopySrc,
		};
		Tex = device.W.DeviceCreateTexture(device.Dev, ref td);
		View = device.W.TextureCreateView(Tex, null);
		var dd = new TextureDescriptor
		{
			Size = new Extent3D((uint)width, (uint)height, 1), Format = WebGpuDevice.DepthStencilFormat,
			MipLevelCount = 1, SampleCount = 1, Dimension = TextureDimension.Dimension2D, Usage = TextureUsage.RenderAttachment,
		};
		DepthTex = device.W.DeviceCreateTexture(device.Dev, ref dd);
		DepthView = device.W.TextureCreateView(DepthTex, null);
	}
}

internal sealed class PathFill
{
	public float[] FanDevice;
	public Vector2 BbMin, BbMax;
	public WColor Color;
}

public sealed class WebGpuRenderData : IRenderData
{
	internal List<(WColor color, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)> Rects = new();
	internal List<PathFill> Paths = new();
	internal WColor? ClearColor;
	public void Dispose() { Rects = null; Paths = null; }
}

public sealed class WebGpuCommandRecorder : ICommandRecorder, IFlattenedPathSink
{
	private readonly Stack<Matrix4x4> _stack = new();
	private Matrix4x4 _m = Matrix4x4.Identity;
	private readonly WebGpuRenderData _data = new();

	public Matrix4x4 TotalMatrix => _m;
	public void SetMatrix(in Matrix4x4 matrix) => _m = matrix;
	public void Concat(in Matrix4x4 matrix) => _m = matrix * _m;
	public void Translate(float dx, float dy) => _m = Matrix4x4.CreateTranslation(dx, dy, 0) * _m;
	public void Scale(float sx, float sy) => _m = Matrix4x4.CreateScale(sx, sy, 1) * _m;
	public int Save() { _stack.Push(_m); return _stack.Count; }
	public int SaveCount => _stack.Count;
	public void Restore() { if (_stack.Count > 0) _m = _stack.Pop(); }
	public void RestoreToCount(int count) { while (_stack.Count > count) _m = _stack.Pop(); }
	public void SaveLayer(bool antialias = false) => Save();
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false) => Save();
	public void SaveLayer(BlendMode blendMode, bool antialias = false) => Save();
	public void SaveLayer(IEffectFilter filter) => Save();
	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void Clear(WColor color) => _data.ClearColor = color;

	private Vector2 Map(float x, float y) => new(x * _m.M11 + y * _m.M21 + _m.M41, x * _m.M12 + y * _m.M22 + _m.M42);

	public void DrawRect(in Rect rect, WColor color, bool antialias = false)
		=> _data.Rects.Add((color, Map((float)rect.Left, (float)rect.Top), Map((float)rect.Right, (float)rect.Top),
			Map((float)rect.Right, (float)rect.Bottom), Map((float)rect.Left, (float)rect.Bottom)));

	private List<float> _fan;
	private Vector2 _pivot, _prev, _bbMin, _bbMax;
	private bool _firstInContour;

	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false)
	{
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		geometry.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_data.Paths.Add(new PathFill { FanDevice = _fan.ToArray(), BbMin = _bbMin, BbMax = _bbMax, Color = color });
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

	public void DrawRect(in Rect rect, IShader shader, bool antialias = false) { }
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false) { }
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false) { }
	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) { }
	public void DrawImageNineSlice(IImage image, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false) { }
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) { }

	public IRenderData Finish() => _data;
	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();
	public void Replay(IRenderData data) { }
}

public sealed unsafe class WebGpuPresentSession : IPresentSession
{
	private readonly WebGpuDevice _d;
	private readonly WebGpuRenderSurface _s;
	public WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s) { _d = d; _s = s; }

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

		var rectV = new List<float>();
		foreach (var (col, p0, p1, p2, p3) in rd.Rects)
		{
			var c = new Vector4(col.R / 255f, col.G / 255f, col.B / 255f, col.A / 255f);
			void V(Vector2 p) { var n = Ndc(p); rectV.Add(n.X); rectV.Add(n.Y); rectV.Add(c.X); rectV.Add(c.Y); rectV.Add(c.Z); rectV.Add(c.W); }
			V(p0); V(p1); V(p2); V(p0); V(p2); V(p3);
		}
		Silk.NET.WebGPU.Buffer* rectBuf = rectV.Count > 0 ? MakeBuffer(rectV.ToArray()) : null;

		var pathBufs = new List<(nint fan, uint fanCount, nint cover)>();
		foreach (var pf in rd.Paths)
		{
			var fanNdc = new float[pf.FanDevice.Length];
			for (int i = 0; i < pf.FanDevice.Length; i += 2) { var n = Ndc(new Vector2(pf.FanDevice[i], pf.FanDevice[i + 1])); fanNdc[i] = n.X; fanNdc[i + 1] = n.Y; }
			var fanBuf = MakeBuffer(fanNdc);
			var c = new Vector4(pf.Color.R / 255f, pf.Color.G / 255f, pf.Color.B / 255f, pf.Color.A / 255f);
			var cov = new List<float>();
			void CV(Vector2 p) { var n = Ndc(p); cov.Add(n.X); cov.Add(n.Y); cov.Add(c.X); cov.Add(c.Y); cov.Add(c.Z); cov.Add(c.W); }
			var tl = pf.BbMin; var br = pf.BbMax; var tr = new Vector2(br.X, tl.Y); var bl = new Vector2(tl.X, br.Y);
			CV(tl); CV(tr); CV(br); CV(tl); CV(br); CV(bl);
			pathBufs.Add(((nint)fanBuf, (uint)(pf.FanDevice.Length / 2), (nint)MakeBuffer(cov.ToArray())));
		}

		var enc = W.DeviceCreateCommandEncoder(_d.Dev, null);
		var clear = rd.ClearColor;
		var ca = new RenderPassColorAttachment
		{
			View = _s.View, LoadOp = clear.HasValue ? LoadOp.Clear : LoadOp.Load, StoreOp = StoreOp.Store,
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

		if (rectBuf != null)
		{
			W.RenderPassEncoderSetPipeline(pass, _d.SolidPipe);
			W.RenderPassEncoderSetVertexBuffer(pass, 0, rectBuf, 0, (nuint)(rectV.Count * sizeof(float)));
			W.RenderPassEncoderDraw(pass, (uint)(rectV.Count / 6), 1, 0, 0);
		}

		foreach (var (fan, fanCount, cover) in pathBufs)
		{
			W.RenderPassEncoderSetPipeline(pass, _d.StencilPipe);
			W.RenderPassEncoderSetVertexBuffer(pass, 0, (Silk.NET.WebGPU.Buffer*)fan, 0, (nuint)(fanCount * 2 * sizeof(float)));
			W.RenderPassEncoderDraw(pass, fanCount, 1, 0, 0);

			W.RenderPassEncoderSetPipeline(pass, _d.CoverPipe);
			W.RenderPassEncoderSetStencilReference(pass, 0);
			W.RenderPassEncoderSetVertexBuffer(pass, 0, (Silk.NET.WebGPU.Buffer*)cover, 0, (nuint)(36 * sizeof(float)));
			W.RenderPassEncoderDraw(pass, 6, 1, 0, 0);
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
	public void Clear(WColor color) { }
	public void DrawRect(in Rect rect, WColor color, bool antialias = false) { }
	public void DrawRect(in Rect rect, IShader shader, bool antialias = false) { }
	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false) { }
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false) { }
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false) { }
	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) { }
	public void DrawImageNineSlice(IImage image, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false) { }
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) { }
	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();
	public void Dispose() { }
}

public sealed class WebGpuRenderBackend : IRenderBackend
{
	public readonly WebGpuDevice Device;
	public WebGpuRenderBackend(WebGpuDevice device) => Device = device;
	public ICommandRecorder BeginFrame() => new WebGpuCommandRecorder();
	public IPresentSession BeginPresent(IRenderSurface target) => new WebGpuPresentSession(Device, (WebGpuRenderSurface)target);
}
