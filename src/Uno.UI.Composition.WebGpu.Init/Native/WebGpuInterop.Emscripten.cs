// Emscripten/Dawn-only additions to the generated binding. emdawnwebgpu implements Dawn's webgpu.h, which
// has an HTML-<canvas> surface source that wgpu-native's header (the generator's input) does not — so it's
// supplemented by hand here instead of being emitted. Used only on WASM to bind a WGPUSurface to a canvas.
#nullable disable
using System;
using System.Runtime.InteropServices;

namespace Uno.WebGpu.Native;

#pragma warning disable CA1815 // native ABI struct; equality is meaningless
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUEmscriptenSurfaceSourceCanvasHTMLSelector
{
	public WGPUChainedStruct Chain;
	public WGPUStringView Selector;
}
#pragma warning restore CA1815

public static partial class WGPU
{
	// WGPUSType_EmscriptenSurfaceSourceCanvasHTMLSelector (Dawn); not a member of the wgpu-native-derived enum.
	public const WGPUSType SType_EmscriptenSurfaceSourceCanvasHTMLSelector = (WGPUSType)0x00040000;
}
