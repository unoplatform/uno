// Resolves the "webgpu" DllImport to the WebGPU library the app ships: Dawn, under the DllImport's own name (webgpu.dll /
// libwebgpu.so / libwebgpu.dylib) so default resolution finds it, or wgpu-native when UnoWebGpuNative=WgpuNative selects
// it, under its own name (wgpu_native.dll / libwgpu_native.*). This resolver covers what default resolution can't: the
// wgpu-native name, the runtimes/<rid>/native folder of an app built for several platforms, Apple static linking (symbols
// in the main program image) and Android. Registered via a module initializer so it's active before any P/Invoke.
#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

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

	/// <summary>The library the "webgpu" [DllImport]s bound to, once one has been called.</summary>
	internal static IntPtr Resolved { get; private set; }

	// UNO_WEBGPU_LIBRARY: a path to the WebGPU library to load instead of the provisioned one, either engine. Read on
	// first use, not at module initialization: Android sets UNO_* variables from the launch intent in OnCreate.
	private static readonly Lazy<string?> _override = new(() => OperatingSystem.IsBrowser() ? null : Environment.GetEnvironmentVariable("UNO_WEBGPU_LIBRARY") is { Length: > 0 } path ? path : null);

	/// <summary>
	/// Whether the native library is Dawn rather than wgpu-native. Both implement webgpu.h, but only wgpu-native has its
	/// own extensions (wgpuDevicePoll among them), which the binding stands in for on Dawn. Loads the library to tell.
	/// </summary>
	internal static bool IsDawn => _isDawn.Value;

	private static readonly Lazy<bool> _isDawn = new(() =>
	{
		if (OperatingSystem.IsBrowser())
		{
			return false;
		}
		var library = Resolved != IntPtr.Zero ? Resolved : NativeLibrary.Load("webgpu", typeof(WGPU).Assembly, null);
		return !NativeLibrary.TryGetExport(library, "wgpuDevicePoll", out _);
	});

	private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		var handle = ResolveCore(libraryName);
		if (handle != IntPtr.Zero)
		{
			Resolved = handle;
		}
		return handle;
	}

	private static IntPtr ResolveCore(string libraryName)
	{
		if (libraryName != "webgpu")
		{
			return IntPtr.Zero;
		}

		if (_override.Value is { } path)
		{
			return NativeLibrary.Load(path);
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

		// Android packs the native per-ABI: Dawn as libwebgpu.so, wgpu-native under its own name, libwgpu_native.so.
		// Resolve either by bare name through the Android loader.
		if (OperatingSystem.IsAndroid())
		{
			foreach (var candidate in new[] { "webgpu", "libwebgpu", "wgpu_native", "libwgpu_native" })
			{
				if (NativeLibrary.TryLoad(candidate, out var handle))
				{
					return handle;
				}
			}
		}

		// Desktop: next to the app, or under runtimes/<rid>/native for an app built on one OS for every desktop platform
		// (the names collide across architectures, so they cannot all sit next to the app). Dawn first, then wgpu-native.
		if (!OperatingSystem.IsBrowser() && TryLoadDesktop() is var desktop && desktop != IntPtr.Zero)
		{
			return desktop;
		}

		// Otherwise decline so the runtime's default resolution handles "webgpu" (desktop finds the
		// DllImport-named artifact; WASM resolves the emdawnwebgpu-linked symbols).
		return IntPtr.Zero;
	}

	private static IntPtr TryLoadDesktop()
	{
		var (dawn, wgpu) = OperatingSystem.IsWindows() ? ("webgpu.dll", "wgpu_native.dll")
			: OperatingSystem.IsMacOS() ? ("libwebgpu.dylib", "libwgpu_native.dylib")
			: ("libwebgpu.so", "libwgpu_native.so");
		var ridFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native");
		foreach (var file in new[] { dawn, wgpu })
		{
			foreach (var folder in new[] { AppContext.BaseDirectory, ridFolder })
			{
				var path = System.IO.Path.Combine(folder, file);
				if (System.IO.File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
				{
					return handle;
				}
			}
		}
		return IntPtr.Zero;
	}
}
