// Hand-added wgpu-native extension structs the generator skips (WGPUNativeDisplayHandle carries an
// anonymous union, which prunes WGPUInstanceExtras with it — see gen_webgpu.py). Layouts mirror wgpu.h
// (v29): the union is expressed as its largest member (pointer + int), giving identical size/alignment.
#nullable disable
#pragma warning disable IDE0055 // hand-maintained ABI supplement; see gen_webgpu.py
using System;
using System.Runtime.InteropServices;

namespace Uno.WebGpu.Native;

#pragma warning disable CA1815 // native ABI structs; equality is meaningless
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUNativeDisplayHandle
{
	public WGPUNativeDisplayHandleType Type;
	// Tagged union { xlib, xcb, wayland }: a display/connection pointer plus the X11 screen number.
	public IntPtr Display;
	public int Screen;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WGPUInstanceExtras
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
#pragma warning restore CA1815
