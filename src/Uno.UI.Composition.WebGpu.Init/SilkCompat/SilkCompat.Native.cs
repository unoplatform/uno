// The stable-header declarations the generated binding (SilkCompat.Native.Generated.cs) cannot produce.
#nullable disable
using System;
using System.Runtime.InteropServices;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

#pragma warning disable CA1815 // native ABI structs; equality is meaningless

// wgpu.h's WGPUNativeDisplayHandle holds an anonymous union, which the generator skips along with WGPUInstanceExtras
// that embeds it. The union is laid out as its largest member (pointer + int), so size and alignment match.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WGPUNativeDisplayHandle
{
	public WGPUNativeDisplayHandleType Type;
	// Tagged union { xlib, xcb, wayland }: a display/connection pointer plus the X11 screen number.
	public IntPtr Display;
	public int Screen;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WGPUInstanceExtras
{
	public WGPUChainedStruct Chain; // SType = WGPUNativeSType.WGPUSType_InstanceExtras
	public WGPUInstanceBackend Backends;
	public WGPUInstanceFlag Flags;
	public WGPUDx12Compiler Dx12ShaderCompiler;
	public WGPUGles3MinorVersion Gles3MinorVersion;
	public WGPUGLFenceBehaviour GlFenceBehaviour;
	public WGPUStringView DxcPath;
	public WGPUDxcMaxShaderModel DxcMaxShaderModel;
	public WGPUDx12SwapchainKind Dx12PresentationSystem;
	public IntPtr BudgetForDeviceCreation;
	public IntPtr BudgetForDeviceLoss;
	public WGPUNativeDisplayHandle DisplayHandle;
}

// emdawnwebgpu (Dawn's webgpu.h) has an HTML <canvas> surface source that wgpu-native's header does not.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WGPUEmscriptenSurfaceSourceCanvasHTMLSelector
{
	public WGPUChainedStruct Chain;
	public WGPUStringView Selector;
}

#pragma warning restore CA1815

internal static partial class WGPU
{
	// WGPUSType_EmscriptenSurfaceSourceCanvasHTMLSelector (Dawn); not a member of the wgpu-native-derived enum.
	public const WGPUSType SType_EmscriptenSurfaceSourceCanvasHTMLSelector = (WGPUSType)0x00040000;
}
