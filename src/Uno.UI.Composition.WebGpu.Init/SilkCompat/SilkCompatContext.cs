#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

/// <summary>
/// The function table Silk.NET.WebGPU 2.23 loads from: functions whose ABI the stable webgpu.h kept are the native
/// exports themselves, the rest are translators onto the stable header.
/// </summary>
/// <example><c>var wgpu = new WebGPU(SilkCompatContext.Instance);</c></example>
internal sealed unsafe class SilkCompatContext : INativeContext
{
	public static SilkCompatContext Instance { get; } = new();

	/// <summary>Silk.NET.WebGPU's API, bound to this context.</summary>
	public static WebGPU Api { get; } = new(Instance);

	/// <summary>Silk.NET's wgpu-native extensions, bound to this context.</summary>
	public static Wgpu Wgpu { get; } = new(Instance);

	/// <summary>
	/// A texture format as Silk.NET.WebGPU numbers it, from a stable-header (wgpu-native, emdawnwebgpu) value - for a
	/// format a native API hands out, such as a render target's.
	/// </summary>
	public static TextureFormat ToSilkTextureFormat(uint format) => (TextureFormat)Xlate.N2O_TextureFormat((int)format);

	private readonly Dictionary<string, nint> _table = new();

	private SilkCompatContext()
	{
		if (OperatingSystem.IsBrowser())
		{
			// The natives are linked into the module; every entry is a forwarder to their [DllImport].
			Xlate.Populate(_table, _ => 0);
			return;
		}

		// The passthrough entries must come from the very library the translators' [DllImport]s bound to: loading
		// "webgpu" separately can pick another copy of the native (a self-contained app ships one in its root and one
		// under runtimes/), which is a second wgpu instance whose objects the first can't use. A side-effect-free call
		// binds the [DllImport]s first.
		WGPUInstanceLimits limits = default;
		_ = WGPU.wgpuGetInstanceLimits(&limits);
		var library = WebGpuLoader.Resolved != IntPtr.Zero ? WebGpuLoader.Resolved : NativeLibrary.Load("webgpu", typeof(WGPU).Assembly, null);
		nint Export(string name) => NativeLibrary.TryGetExport(library, name, out var address) ? address : 0;
		WGPU.BindExtras(Export);
		Xlate.Populate(_table, Export);
	}

	public nint GetProcAddress(string proc, int? slot = default)
		=> TryGetProcAddress(proc, out var address, slot)
			? address
			: throw new EntryPointNotFoundException($"Silk.NET compatibility: {proc} is not available.");

	public bool TryGetProcAddress(string proc, out nint address, int? slot = default)
		=> _table.TryGetValue(proc, out address) && address != 0;

	public void Dispose()
	{
	}
}

internal static partial class Xlate
{
	internal static void Populate(Dictionary<string, nint> table, Func<string, nint> native)
	{
		AddGenerated(table, native);
		AddHand(table);
	}
}
