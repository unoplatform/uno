#nullable disable

using System;
using System.Collections.Generic;
using Uno.Dawn;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

// On Dawn, the binding's wgpu-native calls go through the Uno.Dawn package (DawnLibrary): it creates the instance and
// stands in for the wgpu-native extensions the backend calls.
internal static unsafe partial class Xlate
{
	// UNO_WEBGPU_BACKENDS picks the backend on Dawn too, through the adapter request rather than wgpu-native's
	// instance extras (Dawn rejects those).
	private static WGPUBackendType DawnBackend => WebGpuLoader.IsDawn ? _dawnBackend.Value : WGPUBackendType.Undefined;

	private static readonly Lazy<WGPUBackendType> _dawnBackend = new(() =>
		(Environment.GetEnvironmentVariable("UNO_WEBGPU_BACKENDS") ?? "").Split(',')[0].Trim().ToLowerInvariant() switch
		{
			"vulkan" => WGPUBackendType.Vulkan,
			"dx12" => WGPUBackendType.D3D12,
			"dx11" => WGPUBackendType.D3D11,
			"metal" => WGPUBackendType.Metal,
			"gl" => WGPUBackendType.OpenGLES,
			_ => WGPUBackendType.Undefined,
		});

	private static void AddDawn(Dictionary<string, nint> t)
	{
		foreach (var (name, function) in DawnLibrary.GetWgpuNativeFunctions(WebGpuLoader.Library))
		{
			t[name] = function;
		}
	}
}
