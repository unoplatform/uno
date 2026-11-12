#if __SKIA__ && UNO_DRAWING_WEBGPU
#nullable enable
using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Silk.NET.Core.Native;
using Silk.NET.WebGPU;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics3DWebGpu;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", Name = "WebGpuCanvasElement_Triangle", IgnoreInSnapshotTests = true, IsManualTest = true,
		Description = "Shows a rotating triangle drawn by a WebGpuCanvasElement. Run with the WebGPU backend (UNO_WEBGPU=1); otherwise the text reports that WebGPU is unavailable.")]
	public sealed partial class WebGpuCanvasElement_Triangle : UserControl
	{
		public WebGpuCanvasElement_Triangle()
		{
			var canvas = new WebGpuCanvasElement_RotatingTriangle();
			var status = new TextBlock { Margin = new Thickness(8) };
			canvas.RegisterPropertyChangedCallback(
				WebGpuCanvasElement.IsWebGpuInitializedProperty,
				(_, _) => status.Text = $"IsWebGpuInitialized: {canvas.IsWebGpuInitialized?.ToString() ?? "null"}");

			Content = new Grid { Children = { canvas, status } };
		}
	}

	public sealed unsafe class WebGpuCanvasElement_RotatingTriangle : WebGpuCanvasElement
	{
		private const string Shader = """
			struct Uniforms { angle: f32, aspect: f32 }
			@group(0) @binding(0) var<uniform> u: Uniforms;

			struct VsOut { @builtin(position) position: vec4f, @location(0) color: vec3f }

			@vertex fn vs(@builtin(vertex_index) i: u32) -> VsOut {
				var corners = array<vec2f, 3>(vec2f(0.0, 0.6), vec2f(-0.6, -0.45), vec2f(0.6, -0.45));
				var colors = array<vec3f, 3>(vec3f(1.0, 0.2, 0.2), vec3f(0.2, 1.0, 0.2), vec3f(0.2, 0.4, 1.0));
				let c = cos(u.angle);
				let s = sin(u.angle);
				let p = corners[i];
				var out: VsOut;
				out.position = vec4f((p.x * c - p.y * s) / u.aspect, p.x * s + p.y * c, 0.0, 1.0);
				out.color = colors[i];
				return out;
			}

			@fragment fn fs(in: VsOut) -> @location(0) vec4f { return vec4f(in.color, 1.0); }
			""";

		private readonly Stopwatch _clock = Stopwatch.StartNew();
		private RenderPipeline* _pipeline;
		private Silk.NET.WebGPU.Buffer* _uniforms;
		private BindGroup* _bindGroup;

		protected override void Init(WebGpuCanvasContext context)
		{
			var wgpu = context.Api;
			var code = (byte*)SilkMarshal.StringToPtr(Shader);
			var vs = (byte*)SilkMarshal.StringToPtr("vs");
			var fs = (byte*)SilkMarshal.StringToPtr("fs");
			try
			{
				var wgsl = new ShaderModuleWGSLDescriptor { Chain = new ChainedStruct { SType = SType.ShaderModuleWgslDescriptor }, Code = code };
				var moduleDescriptor = new ShaderModuleDescriptor { NextInChain = &wgsl.Chain };
				var module = wgpu.DeviceCreateShaderModule(context.Device, &moduleDescriptor);

				var target = new ColorTargetState { Format = context.TargetFormat, WriteMask = ColorWriteMask.All };
				var fragment = new FragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
				var pipelineDescriptor = new RenderPipelineDescriptor
				{
					Vertex = new VertexState { Module = module, EntryPoint = vs },
					Fragment = &fragment,
					Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, FrontFace = FrontFace.Ccw, CullMode = CullMode.None },
					Multisample = new MultisampleState { Count = 1, Mask = uint.MaxValue },
				};
				_pipeline = wgpu.DeviceCreateRenderPipeline(context.Device, &pipelineDescriptor);
				wgpu.ShaderModuleRelease(module);
			}
			finally
			{
				SilkMarshal.Free((nint)code);
				SilkMarshal.Free((nint)vs);
				SilkMarshal.Free((nint)fs);
			}

			var bufferDescriptor = new BufferDescriptor { Size = 16, Usage = BufferUsage.Uniform | BufferUsage.CopyDst };
			_uniforms = wgpu.DeviceCreateBuffer(context.Device, &bufferDescriptor);

			var layout = wgpu.RenderPipelineGetBindGroupLayout(_pipeline, 0);
			var entry = new BindGroupEntry { Binding = 0, Buffer = _uniforms, Size = 16 };
			var bindGroupDescriptor = new BindGroupDescriptor { Layout = layout, EntryCount = 1, Entries = &entry };
			_bindGroup = wgpu.DeviceCreateBindGroup(context.Device, &bindGroupDescriptor);
			wgpu.BindGroupLayoutRelease(layout);
		}

		protected override void OnDestroy(WebGpuCanvasContext context)
		{
			var wgpu = context.Api;
			if (_bindGroup != null) { wgpu.BindGroupRelease(_bindGroup); }
			if (_uniforms != null) { wgpu.BufferRelease(_uniforms); }
			if (_pipeline != null) { wgpu.RenderPipelineRelease(_pipeline); }
			_bindGroup = null;
			_uniforms = null;
			_pipeline = null;
		}

		protected override void RenderOverride(WebGpuCanvasContext context)
		{
			var wgpu = context.Api;
			var uniforms = stackalloc float[] { (float)_clock.Elapsed.TotalSeconds, context.PixelWidth / (float)context.PixelHeight, 0, 0 };
			wgpu.QueueWriteBuffer(context.Queue, _uniforms, 0, uniforms, 16);

			var encoder = wgpu.DeviceCreateCommandEncoder(context.Device, null);
			var attachment = new RenderPassColorAttachment
			{
				View = context.TargetView,
				DepthSlice = uint.MaxValue, // WGPU_DEPTH_SLICE_UNDEFINED: the target is a 2D texture
				LoadOp = LoadOp.Clear,
				StoreOp = StoreOp.Store,
				ClearValue = new Color(0.1, 0.1, 0.15, 1),
			};
			var passDescriptor = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &attachment };
			var pass = wgpu.CommandEncoderBeginRenderPass(encoder, &passDescriptor);
			wgpu.RenderPassEncoderSetPipeline(pass, _pipeline);
			wgpu.RenderPassEncoderSetBindGroup(pass, 0, _bindGroup, 0, null);
			wgpu.RenderPassEncoderDraw(pass, 3, 1, 0, 0);
			wgpu.RenderPassEncoderEnd(pass);
			wgpu.RenderPassEncoderRelease(pass);

			var commands = wgpu.CommandEncoderFinish(encoder, null);
			wgpu.QueueSubmit(context.Queue, 1, &commands);
			wgpu.CommandBufferRelease(commands);
			wgpu.CommandEncoderRelease(encoder);

			Invalidate();
		}
	}
}
#endif
