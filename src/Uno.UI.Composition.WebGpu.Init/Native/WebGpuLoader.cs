// <auto-provisioning loader> Resolves the "webgpu" DllImport to the pinned wgpu-native (modern ABI) placed
// next to the app by wgpu-native.targets. Registered via a module initializer so it's active before any P/Invoke.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
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
		// wgpu-native xcframework with force-load; see wgpu-native.targets iOS provisioning.)
		if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst())
		{
			try { return NativeLibrary.GetMainProgramHandle(); }
			catch { /* fall through to the file-based candidates below */ }
		}

		foreach (var candidate in Candidates(assembly))
		{
			if (NativeLibrary.TryLoad(candidate, out var handle))
			{
				return handle;
			}
		}
		return IntPtr.Zero;
	}

	private static IEnumerable<string> Candidates(Assembly assembly)
	{
		string file =
			RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "wgpu_native.dll" :
			RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libwgpu_native.dylib" :
			"libwgpu_native.so";

		// The app base dir and the assembly's own dir aren't on the OS loader path on Linux/macOS, so try full paths.
		foreach (var dir in new[] { AppContext.BaseDirectory, Path.GetDirectoryName(assembly.Location) })
		{
			if (!string.IsNullOrEmpty(dir))
			{
				yield return Path.Combine(dir!, file);
			}
		}
		// Bare names as a fallback (honors LD_LIBRARY_PATH / rpath / Windows app-dir search).
		yield return "wgpu_native";
		yield return "libwgpu_native";
	}
}
