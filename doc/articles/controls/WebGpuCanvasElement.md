---
uid: Uno.Controls.WebGpuCanvasElement
---

# WebGpuCanvasElement

> [!IMPORTANT]
> This functionality is only available when the app renders with the WebGPU backend. With any other backend, the element doesn't render, and it reports this through `IsWebGpuInitialized` and `OnWebGpuUnavailable`. It isn't available on WinAppSDK.

`WebGpuCanvasElement` is a control for drawing with your own WebGPU commands. It can be enabled by adding the `WebGpuCanvas` [UnoFeature](xref:Uno.Features.Uno.Sdk), which also adds the WebGPU backend. The app still has to select that backend on its host builder:

```csharp
builder.GraphicsBackend(Uno.UI.Composition.WebGpu.WebGpuBackend.CreateGraphicsProvider());
```

The element renders on the same WebGPU device as the rest of the app. Your commands render into a texture, and the backend draws that texture in the element's place with no copy.

## The Silk.NET.WebGPU API

The element hands you objects of [Silk.NET.WebGPU](https://github.com/dotnet/Silk.NET), a C# binding of the standard `webgpu.h` C API. `WebGpuCanvasContext.Api` is the `WebGPU` instance to call, already bound to the backend's native library; don't create your own with `WebGPU.GetApi()`.

- Each WebGPU object is a pointer to an opaque Silk.NET type, such as `Device*`, `Buffer*` or `RenderPassEncoder*`, so rendering code is `unsafe`. Each `webgpu.h` function is a method of `Api`; for example, `wgpuDeviceCreateBuffer` becomes `Api.DeviceCreateBuffer`.
- Descriptors are structs. Strings are null-terminated UTF-8 `byte*`, which `SilkMarshal.StringToPtr` creates.
- You own what you create: release each object with its `Release` method, such as `Api.BufferRelease`.

Silk.NET.WebGPU 2.23 binds the 2024 version of `webgpu.h`. Uno translates its calls to the stable header that the backend's native library implements, so features added to WebGPU after that version can't be used through it.

## Using the WebGpuCanvasElement

To use `WebGpuCanvasElement`, create a subclass of `WebGpuCanvasElement` and override the abstract methods `Init`, `RenderOverride` and `OnDestroy`.

```csharp
protected abstract void Init(WebGpuCanvasContext context);
protected abstract void RenderOverride(WebGpuCanvasContext context);
protected abstract void OnDestroy(WebGpuCanvasContext context);
```

The `WebGpuCanvasContext` carries the backend's `Device` and `Queue`, plus the `Instance` and `Adapter` (null on WebAssembly, where JavaScript creates the device). It also describes the texture the element displays:

- `Target`, and `TargetView` to use as a render pass color attachment
- `TargetFormat`, which render pipelines that draw into the target must use
- `PixelWidth` and `PixelHeight`, the element's size in physical pixels

The target is only valid during `RenderOverride`. The element holds its own references to the context's objects, so don't release them.

- **`Init`** is called once the element is first drawn with the WebGPU backend. Create your pipelines, buffers and bind groups there. The target texture doesn't exist yet when `Init` runs.
- **`OnDestroy`** is called when the element is unloaded. Release what `Init` created there.
- **`RenderOverride`** encodes commands that render into `TargetView` and submits them to `Queue`. The target keeps its content between calls, so clear it with the render pass's load operation if you redraw everything. The backend draws the target as premultiplied-alpha color.

`Init` and `OnDestroy` may be called multiple times, in pairs.

`RenderOverride` is called once after each call to `Invalidate`, and again whenever the element's size changes. Its output is reused for later frames. To animate, call `Invalidate` inside `RenderOverride`. `Invalidate` is safe to call from any thread.

`IsWebGpuInitialized` is null until the element is first drawn and after it's unloaded. It becomes true once `Init` succeeds. It becomes false when the app isn't running the WebGPU backend, or when an exception escapes `Init` or `RenderOverride`; after an exception, the element stops rendering until it's reloaded. The property is read-only.

## Example

Here's a complete element that draws a triangle with a WGSL shader:

```csharp
using Silk.NET.Core.Native;
using Silk.NET.WebGPU;
using Uno.WinUI.Graphics3DWebGpu;

public sealed unsafe class TriangleCanvas : WebGpuCanvasElement
{
    private const string Shader = """
        @vertex fn vs(@builtin(vertex_index) i: u32) -> @builtin(position) vec4f {
            var corners = array<vec2f, 3>(vec2f(0.0, 0.6), vec2f(-0.6, -0.45), vec2f(0.6, -0.45));
            return vec4f(corners[i], 0.0, 1.0);
        }

        @fragment fn fs() -> @location(0) vec4f { return vec4f(0.2, 0.4, 1.0, 1.0); }
        """;

    private RenderPipeline* _pipeline;

    protected override void Init(WebGpuCanvasContext context)
    {
        var wgpu = context.Api;
        var code = (byte*)SilkMarshal.StringToPtr(Shader);
        var vs = (byte*)SilkMarshal.StringToPtr("vs");
        var fs = (byte*)SilkMarshal.StringToPtr("fs");

        var wgsl = new ShaderModuleWGSLDescriptor { Chain = new ChainedStruct { SType = SType.ShaderModuleWgslDescriptor }, Code = code };
        var moduleDescriptor = new ShaderModuleDescriptor { NextInChain = &wgsl.Chain };
        var module = wgpu.DeviceCreateShaderModule(context.Device, &moduleDescriptor);

        var target = new ColorTargetState { Format = context.TargetFormat, WriteMask = ColorWriteMask.All };
        var fragment = new FragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
        var descriptor = new RenderPipelineDescriptor
        {
            Vertex = new VertexState { Module = module, EntryPoint = vs },
            Fragment = &fragment,
            Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList },
            Multisample = new MultisampleState { Count = 1, Mask = uint.MaxValue },
        };
        _pipeline = wgpu.DeviceCreateRenderPipeline(context.Device, &descriptor);

        wgpu.ShaderModuleRelease(module);
        SilkMarshal.Free((nint)code);
        SilkMarshal.Free((nint)vs);
        SilkMarshal.Free((nint)fs);
    }

    protected override void OnDestroy(WebGpuCanvasContext context) => context.Api.RenderPipelineRelease(_pipeline);

    protected override void RenderOverride(WebGpuCanvasContext context)
    {
        var wgpu = context.Api;
        var encoder = wgpu.DeviceCreateCommandEncoder(context.Device, null);
        var attachment = new RenderPassColorAttachment
        {
            View = context.TargetView,
            DepthSlice = uint.MaxValue, // the target is a 2D texture
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
            ClearValue = new Color(0.1, 0.1, 0.15, 1),
        };
        var passDescriptor = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &attachment };
        var pass = wgpu.CommandEncoderBeginRenderPass(encoder, &passDescriptor);
        wgpu.RenderPassEncoderSetPipeline(pass, _pipeline);
        wgpu.RenderPassEncoderDraw(pass, 3, 1, 0, 0);
        wgpu.RenderPassEncoderEnd(pass);
        wgpu.RenderPassEncoderRelease(pass);

        var commands = wgpu.CommandEncoderFinish(encoder, null);
        wgpu.QueueSubmit(context.Queue, 1, &commands);
        wgpu.CommandBufferRelease(commands);
        wgpu.CommandEncoderRelease(encoder);
    }
}
```
