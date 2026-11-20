#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Linux;

// libHarfBuzzSharp is, at the time of writing, almost but not exactly identical to libharfbuzz.so.0 that ships with
// most Linux environments and, unlike libSkiaSharp, does not have any extra helper functions to help with marshalling, etc.
// However, we need to avoid symbol clashes when other dependencies also depend on harfbuzz. Concretely, when a
// WebView is loaded, it loads libgtk-3 which in turn depends on libharfbuzz.so.0. Afterward, we can get crashes
// when SkiaSharp makes a HarfBuzz call to symbols that were first resolved by libgtk-3 to be in libharfbuzz.so.0.
// In this scenario, the call starts in libHarfBuzzSharp.so but then jumps to symbols in libharfbuzz.so.0
// (the symbols are also in libHarfBuzzSharp.so, but libharfbuzz.so.0 resolved them first).
internal static partial class LinuxHarfBuzz
{
	private const int RTLD_NOW = 2;
	private const int RTLD_DEEPBIND = 8;

	// HarfBuzzSharp is the font providers' dependency, not the host's: an app whose font provider doesn't use it
	// has nothing to isolate.
	internal static void Isolate(bool useSystemHarfBuzz)
	{
		if (TryLoadHarfBuzzSharp() is not { } harfBuzzSharp)
		{
			return;
		}

		if (useSystemHarfBuzz)
		{
			// We can choose to ignore libHarfBuzzSharp entirely by redirecting all calls to libharfbuzz.so.0.
			NativeLibrary.SetDllImportResolver(harfBuzzSharp, static (libraryName, assembly, searchPath)
				=> libraryName == "libHarfBuzzSharp" && NativeLibrary.TryLoad("libharfbuzz.so.0", assembly, searchPath, out var library) ? library : IntPtr.Zero);
			return;
		}

		// Alternatively, we can preload libHarfBuzzSharp with RTLD_DEEPBIND to ensure its symbols are used.
		try
		{
			var search = AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES")?.ToString() ?? "";
			foreach (var path in search.Split(Path.PathSeparator))
			{
				var libraryPath = Path.Combine(path, "libHarfBuzzSharp.so");
				if (File.Exists(libraryPath) && DeepBind(libraryPath) != IntPtr.Zero)
				{
					return;
				}
			}

			throw new FileNotFoundException($"Could not find libHarfBuzzSharp.so in the configured native DLL search directories. Searched directories: {search}");
		}
		catch (Exception ex)
		{
			typeof(LinuxHarfBuzz).LogError()?.Error($"Could not preload HarfBuzz with RTLD_DEEPBIND: {ex.Message}");
		}
	}

	private static Assembly? TryLoadHarfBuzzSharp()
	{
		try
		{
			return Assembly.Load("HarfBuzzSharp");
		}
		catch (FileNotFoundException)
		{
			return null;
		}
	}

	private static IntPtr DeepBind(string path)
	{
		try
		{
			return dlopen2(path, RTLD_NOW | RTLD_DEEPBIND);
		}
		catch (DllNotFoundException)
		{
			// Distributions without the libdl.so.2 soname.
			return dlopen1(path, RTLD_NOW | RTLD_DEEPBIND);
		}
	}

	[LibraryImport("libdl.so", EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8)]
	private static partial IntPtr dlopen1(string path, int mode);

	[LibraryImport("libdl.so.2", EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8)]
	private static partial IntPtr dlopen2(string path, int mode);
}
