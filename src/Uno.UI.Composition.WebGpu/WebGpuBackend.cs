// Minimal WebGPU backend implementing the NEUTRAL drawing seam (public SPI from Uno.UI.Composition).
// Proves an external assembly can drive rendering via IRenderBackend with zero core changes / no IVT.
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
	public RenderPipeline* Pipe;

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
		CreatePipeline();
	}

	private const string Wgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>) -> VOut {
  var o: VOut; o.p = vec4<f32>(pos, 0.0, 1.0); o.c = col; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return i.c; }";

	private void CreatePipeline()
	{
		var code = (byte*)SilkMarshal.StringToPtr(Wgsl, NativeStringEncoding.UTF8);
		var wgslDesc = new ShaderModuleWGSLDescriptor
		{
			Chain = new ChainedStruct { SType = SType.ShaderModuleWgslDescriptor },
			Code = code,
		};
		var smDesc = new ShaderModuleDescriptor { NextInChain = (ChainedStruct*)&wgslDesc };
		var module = W.DeviceCreateShaderModule(Dev, ref smDesc);

		var vsEntry = (byte*)SilkMarshal.StringToPtr("vs", NativeStringEncoding.UTF8);
		var fsEntry = (byte*)SilkMarshal.StringToPtr("fs", NativeStringEncoding.UTF8);

		var attrs = stackalloc VertexAttribute[2];
		attrs[0] = new VertexAttribute { Format = VertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		attrs[1] = new VertexAttribute { Format = VertexFormat.Float32x4, Offset = 8, ShaderLocation = 1 };
		var vbl = new VertexBufferLayout { ArrayStride = 24, StepMode = VertexStepMode.Vertex, AttributeCount = 2, Attributes = attrs };
		var vsState = new VertexState { Module = module, EntryPoint = vsEntry, BufferCount = 1, Buffers = &vbl };

		var blend = new BlendState
		{
			Color = new BlendComponent { SrcFactor = BlendFactor.SrcAlpha, DstFactor = BlendFactor.OneMinusSrcAlpha, Operation = BlendOperation.Add },
			Alpha = new BlendComponent { SrcFactor = BlendFactor.One, DstFactor = BlendFactor.OneMinusSrcAlpha, Operation = BlendOperation.Add },
		};
		var target = new ColorTargetState { Format = TextureFormat.Rgba8Unorm, Blend = &blend, WriteMask = ColorWriteMask.All };
		var fsState = new FragmentState { Module = module, EntryPoint = fsEntry, TargetCount = 1, Targets = &target };

		var pipeDesc = new RenderPipelineDescriptor
		{
			Vertex = vsState,
			Fragment = &fsState,
			Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, StripIndexFormat = IndexFormat.Undefined, FrontFace = FrontFace.Ccw, CullMode = CullMode.None },
			Multisample = new MultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = false },
			Layout = null,
		};
		Pipe = W.DeviceCreateRenderPipeline(Dev, ref pipeDesc);
	}

	public void Dispose() { }
}

public sealed unsafe class WebGpuRenderSurface : IRenderSurface
{
	public Texture* Tex;
	public TextureView* View;
	public readonly int Width, Height;

	public WebGpuRenderSurface(WebGpuDevice device, int width, int height)
	{
		Width = width; Height = height;
		var td = new TextureDescriptor
		{
			Size = new Extent3D((uint)width, (uint)height, 1), Format = TextureFormat.Rgba8Unorm,
			MipLevelCount = 1, SampleCount = 1, Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment | TextureUsage.CopySrc,
		};
		Tex = device.W.DeviceCreateTexture(device.Dev, ref td);
		View = device.W.TextureCreateView(Tex, null);
	}
}

internal enum CmdKind { Clear, Rect }

internal struct DrawCmd
{
	public CmdKind Kind;
	public WColor Color;
	public Vector2 P0, P1, P2, P3; // device-space quad corners (TL, TR, BR, BL)
}

public sealed class WebGpuRenderData : IRenderData
{
	internal List<DrawCmd> Commands = new();
	internal WColor? ClearColor;
	public void Dispose() => Commands = null;
}

// The recorder: implements the neutral IDrawingSession by accumulating device-space commands.
public sealed class WebGpuCommandRecorder : ICommandRecorder
{
	private readonly Stack<Matrix4x4> _stack = new();
	private Matrix4x4 _m = Matrix4x4.Identity;
	private readonly List<DrawCmd> _cmds = new();
	private WColor? _clear;

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
	public void Clear(WColor color) => _clear = color;

	public void DrawRect(in Rect rect, WColor color, bool antialias = false)
	{
		_cmds.Add(new DrawCmd
		{
			Kind = CmdKind.Rect,
			Color = color,
			P0 = MapPoint((float)rect.Left, (float)rect.Top),
			P1 = MapPoint((float)rect.Right, (float)rect.Top),
			P2 = MapPoint((float)rect.Right, (float)rect.Bottom),
			P3 = MapPoint((float)rect.Left, (float)rect.Bottom),
		});
	}

	private Vector2 MapPoint(float x, float y)
		=> new(x * _m.M11 + y * _m.M21 + _m.M41, x * _m.M12 + y * _m.M22 + _m.M42);

	// Not yet implemented in this minimal milestone (filled during the port).
	public void DrawRect(in Rect rect, IShader shader, bool antialias = false) { }
	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false) { }
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false) { }
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false) { }
	public void DrawImage(IImage image, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) { }
	public void DrawImageNineSlice(IImage image, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false) { }
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) { }

	public IRenderData Finish() => new WebGpuRenderData { Commands = _cmds, ClearColor = _clear };
	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();
	public void Replay(IRenderData data) { }
}

public sealed unsafe class WebGpuPresentSession : IPresentSession
{
	private readonly WebGpuDevice _d;
	private readonly WebGpuRenderSurface _s;
	public WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s) { _d = d; _s = s; }

	public void Replay(IRenderData data)
	{
		var rd = (WebGpuRenderData)data;
		var verts = new List<float>(rd.Commands.Count * 36);
		foreach (var c in rd.Commands)
		{
			if (c.Kind != CmdKind.Rect) continue;
			var col = new Vector4(c.Color.R / 255f, c.Color.G / 255f, c.Color.B / 255f, c.Color.A / 255f);
			// TL, TR, BR, BL → two triangles (TL,TR,BR) (TL,BR,BL)
			AddV(verts, Ndc(c.P0), col); AddV(verts, Ndc(c.P1), col); AddV(verts, Ndc(c.P2), col);
			AddV(verts, Ndc(c.P0), col); AddV(verts, Ndc(c.P2), col); AddV(verts, Ndc(c.P3), col);
		}

		var W = _d.W;
		Silk.NET.WebGPU.Buffer* vbuf = null;
		var vertCount = (uint)(verts.Count / 6);
		var byteSize = (nuint)(verts.Count * sizeof(float));
		if (vertCount > 0)
		{
			var bd = new BufferDescriptor { Size = byteSize, Usage = BufferUsage.Vertex | BufferUsage.CopyDst, MappedAtCreation = false };
			vbuf = W.DeviceCreateBuffer(_d.Dev, ref bd);
			var arr = verts.ToArray();
			fixed (float* p = arr) { W.QueueWriteBuffer(_d.Q, vbuf, 0, p, byteSize); }
		}

		var enc = W.DeviceCreateCommandEncoder(_d.Dev, null);
		var clear = rd.ClearColor;
		var ca = new RenderPassColorAttachment
		{
			View = _s.View,
			LoadOp = clear.HasValue ? LoadOp.Clear : LoadOp.Load,
			StoreOp = StoreOp.Store,
			ClearValue = clear.HasValue ? new Silk.NET.WebGPU.Color(clear.Value.R / 255.0, clear.Value.G / 255.0, clear.Value.B / 255.0, clear.Value.A / 255.0) : default,
		};
		var rp = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca };
		var pass = W.CommandEncoderBeginRenderPass(enc, ref rp);
		if (vertCount > 0)
		{
			W.RenderPassEncoderSetPipeline(pass, _d.Pipe);
			W.RenderPassEncoderSetVertexBuffer(pass, 0, vbuf, 0, byteSize);
			W.RenderPassEncoderDraw(pass, vertCount, 1, 0, 0);
		}
		W.RenderPassEncoderEnd(pass);
		var cb = W.CommandEncoderFinish(enc, null);
		W.QueueSubmit(_d.Q, 1, &cb);
		_d.Native.DevicePoll(_d.Dev, true, null);
	}

	private Vector2 Ndc(Vector2 device) => new(2f * device.X / _s.Width - 1f, 1f - 2f * device.Y / _s.Height);
	private static void AddV(List<float> v, Vector2 p, Vector4 c) { v.Add(p.X); v.Add(p.Y); v.Add(c.X); v.Add(c.Y); v.Add(c.Z); v.Add(c.W); }

	// present session also satisfies IDrawingSession (unused in replay-only flow)
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
