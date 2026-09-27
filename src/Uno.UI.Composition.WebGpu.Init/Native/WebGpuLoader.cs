// <auto-provisioning loader> Resolves the "webgpu" DllImport to the pinned wgpu-native (modern ABI). On desktop the
// native is provisioned under the DllImport's own name (libwebgpu.so / webgpu.dll / libwebgpu.dylib, see
// wgpu-native.targets), so the runtime's default resolution finds it (app dir, runtimes/<rid>/native via deps.json,
// rpath/LD_LIBRARY_PATH) with no explicit probing here. This resolver only covers the cases default resolution can't:
// Apple static linking (symbols in the main program image) and Android (packed as libwgpu_native.so per ABI).
// Registered via a module initializer so it's active before any P/Invoke.
#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Uno.WebGpu.Native;

internal static class WebGpuLoader
{
	private static int _registered;

#pragma warning disable CA2255 // intentional library module initializer
	[ModuleInitializer]
	internal static void Register()
	{
		if (Interlocked.Exchange(ref _registered, 1) != 0)
		{
			return;
		}
		NativeLibrary.SetDllImportResolver(typeof(WGPU).Assembly, Resolve);
	}
#pragma warning restore CA2255

	private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName != "webgpu")
		{
			return IntPtr.Zero;
		}

		// iOS/tvOS (and any statically-linked host): wgpu-native is a static lib linked into the app, not a
		// loadable dylib — Apple platforms forbid dlopen'ing arbitrary dylibs. Its symbols live in the main
		// program image, so resolve "webgpu" to the main-program handle. (Requires the build to link the
		// wgpu-native static lib with force-load; see wgpu-native.targets iOS provisioning.)
		if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst())
		{
			try { return NativeLibrary.GetMainProgramHandle(); }
			catch { /* fall through */ }
		}

		// Android packs the native per-ABI as libwgpu_native.so (its own name, not the DllImport's); resolve it by
		// bare name through the Android loader. On desktop this fails harmlessly and we fall through to Zero, letting
		// default resolution find the DllImport-named artifact (libwebgpu.*).
		if (OperatingSystem.IsAndroid())
		{
			foreach (var candidate in new[] { "wgpu_native", "libwgpu_native" })
			{
				if (NativeLibrary.TryLoad(candidate, out var handle))
				{
					return handle;
				}
			}
		}

		// Desktop and WASM: decline so the runtime's default resolution handles "webgpu" (desktop finds the
		// DllImport-named artifact; WASM resolves the emdawnwebgpu-linked symbols).
		return IntPtr.Zero;
	}
}
