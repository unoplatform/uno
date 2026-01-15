#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Documents;

internal readonly partial struct UnicodeText
{
	private static class ICU
	{
		// The version number of ICU is important because the exported symbols have their names appended by
		// the version number. For example, there's a ubrk_open_74 in ICU v74, but not a ubrk_open.
		private static readonly int _icuVersion;
		private static readonly IntPtr _libicuuc;
		private static readonly Dictionary<Type, object> _lookupCache = new();

		private const DllImportSearchPath NativeLibrarySearchDirectories =
			  DllImportSearchPath.ApplicationDirectory
			| DllImportSearchPath.AssemblyDirectory
			| DllImportSearchPath.UserDirectories
			;

		static unsafe ICU()
		{
			IntPtr libicuuc;
			if (OperatingSystem.IsWindows())
			{
				// On Windows, we get the ICU binaries from the uno.icu-win package.
				_icuVersion = 77;
				if (!NativeLibrary.TryLoad("icuuc77", typeof(ICU).Assembly, NativeLibrarySearchDirectories, out libicuuc))
				{
					throw new Exception("Failed to load libicuuc.");
				}
			}
			else if (OperatingSystem.IsIOS())
			{
				_icuVersion = 77;
				libicuuc = IntPtr.Zero;
			}
			else if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid() || OperatingSystem.IsMacOS())
			{
				// On Linux and Android, we get the ICU binaries from the dynamic linker search path
				// On MacOS, we get the ICU binaries from the uno.icu-macos package.
				if (OperatingSystem.IsMacOS() && !NativeLibrary.TryLoad("icudata", typeof(ICU).Assembly, NativeLibrarySearchDirectories, out _))
				{
					// MacOS doesn't automatically load icudata from icuuc for some reason even though the icuuc binary
					// lists icudata in the `otool -L` output, so we have to load it by hand
					throw new Exception("Failed to load libicudata.");
				}
				if (!NativeLibrary.TryLoad("icuuc", typeof(ICU).Assembly, NativeLibrarySearchDirectories, out libicuuc))
				{
					if (OperatingSystem.IsLinux())
					{
						for (int j = 100; j >= 67; j--)
						{
							// some environments only have a versioned library and don't symlink it to libicuuc.so
							if (NativeLibrary.TryLoad($"libicuuc.so.{j}", typeof(ICU).Assembly, DllImportSearchPath.UserDirectories, out libicuuc))
							{
								break;
							}
						}
					}
					if (libicuuc == IntPtr.Zero)
					{
						throw new Exception("Failed to load libicuuc.");
					}
				}

				// Since libicuuc not installed by us, we have no control over the specific version number, so
				// we try a wide range of versions.
				for (int i = 100; i >= 67; i--)
				{
					if (NativeLibrary.TryGetExport(libicuuc, $"u_getVersion_{i}", out _))
					{
						_icuVersion = i;
					}
				}

				if (_icuVersion == 0)
				{
					throw new Exception("Failed to load icuuc.");
				}
			}
			else if (OperatingSystem.IsBrowser())
			{
				var stream = AppDomain.CurrentDomain
					.GetAssemblies()
					.Select(a => (a, a.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith("icudt.dat", StringComparison.InvariantCulture))))
					.Where(t => t.Item2 != null)
					.Select(t => t.a.GetManifestResourceStream(t.Item2!))
					.First()!;
				// udata_setCommonData does not copy the buffer, so it needs to be pinned.
				// For alignment, the ICU docs require 16-byte alignment. https://unicode-org.github.io/icu/userguide/icu_data/#alignment
				var data = NativeMemory.AlignedAlloc((UIntPtr)stream.Length, 16);
				stream.ReadExactly(new Span<byte>(data, (int)stream.Length));
				var errorPtr = BrowserICUSymbols.uno_udata_setCommonData((IntPtr)data);
				var errorString = Marshal.PtrToStringUTF8(errorPtr);
				if (errorString is not null)
				{
					throw new InvalidOperationException($"uno_udata_setCommonData failed: {errorString}");
				}

				// ICU is included in the unoicu.a static library without version postfixes, so the version doesn't matter
				_icuVersion = 1;
				libicuuc = IntPtr.Zero;
			}
			else
			{
				throw new DllNotFoundException("Failed to load libicuuc: unsupported platform.");
			}

			_libicuuc = libicuuc;

			GetMethod<u_getVersion>()(out var versionInfo);
			var ptr = Marshal.AllocHGlobal(1000);
			GetMethod<u_versionToString>()((IntPtr)(&versionInfo), ptr);
			typeof(ICU).LogDebug()?.Debug($"Found ICU version {Marshal.PtrToStringAnsi(ptr)}.");
			Marshal.FreeHGlobal(ptr);
		}

		public static T GetMethod<T>()
		{
			if (!_lookupCache.TryGetValue(typeof(T), out var value))
			{
				if (OperatingSystem.IsIOS() || OperatingSystem.IsBrowser())
				{
					// iOS doesn't support NativeLibrary.TryGetExport so we have to make DllImport declarations to
					// the exact symbol names at compile times (even DllImport.EntryPoint doesn't work) and do the
					// method mapping by reflection.
					// On WASM, NativeLibrary.TryGetExport is supported, but not on NativeAOT.
					var (methodName, type) = OperatingSystem.IsBrowser() ? ($"uno_{typeof(T).Name}", typeof(BrowserICUSymbols)) : ($"{typeof(T).Name}_{_icuVersion}", typeof(IOSICUSymbols));
					var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
					if (method is null)
					{
						throw new InvalidOperationException($"Failed to find {typeof(T).Name} in {type.Name}.");
					}
					value = Delegate.CreateDelegate(typeof(T), method);
				}
				else if (NativeLibrary.TryGetExport(_libicuuc, typeof(T).Name, out var originalNameFunc))
				{
					value = Marshal.GetDelegateForFunctionPointer<T>(originalNameFunc)!;
				}
				else if (NativeLibrary.TryGetExport(_libicuuc, $"{typeof(T).Name}_{_icuVersion}", out var versionPostfixedFunc))
				{
					value = Marshal.GetDelegateForFunctionPointer<T>(versionPostfixedFunc)!;
				}
				else
				{
					throw new Exception($"Failed to obtain the {typeof(T).Name} method from the ICU libraries.");
				}
				_lookupCache[typeof(T)] = value;
			}
			return (T)value;
		}

		public static unsafe DisposableStruct<IntPtr> CreateBiDiAndSetPara(string text, int start, int end, byte paraLevel, out IntPtr bidi)
		{
			bidi = GetMethod<ubidi_open>()();
			fixed (char* textPtr = &text.GetPinnableReference())
			{
				GetMethod<ubidi_setPara>()(bidi, (IntPtr)(textPtr + start), end - start, paraLevel, IntPtr.Zero, out var setParaErrorCode);
				if (setParaErrorCode > 0)
				{
					throw new InvalidOperationException($"{nameof(ubidi_setPara)} failed with error code {setParaErrorCode}");
				}
			}
			return new DisposableStruct<IntPtr>(static bidi => GetMethod<ubidi_close>()(bidi), bidi);
		}

		public static void CheckErrorCode<T>(int status)
		{
			if (status > 0)
			{
				throw new InvalidOperationException($"{typeof(T).Name} failed with error code {status.ToString("X", CultureInfo.InvariantCulture)}");
			}
			else if (status < 0)
			{
				// ICU has a very low bar for what it considers a "warning", so this can be very spammy. 
				typeof(ICU).LogTrace()?.Warn($"{typeof(T).Name} raised a warning code {status.ToString("X", CultureInfo.InvariantCulture)}");
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr ubidi_open();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ubidi_close(IntPtr pBiDi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ubidi_setPara(IntPtr pBiDi, IntPtr text, int length, byte paraLevel, IntPtr embeddingLevels, out int errorCode);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ubidi_getLogicalRun(IntPtr pBiDi, int logicalPosition, out int logicalLimit, out byte level);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubidi_countRuns(IntPtr pBiDI, out int errorCode);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubidi_getVisualRun(IntPtr pBiDi, int runIndex, out int logicalStart, out int length);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr ubrk_open(int type, IntPtr locale, IntPtr text, int textLength, out int status);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr ubrk_close(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubrk_first(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubrk_next(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void u_getVersion(out UVersionInfo versionInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void u_versionToString(IntPtr versionArray, IntPtr versionString);

		[StructLayout(LayoutKind.Sequential)]
		private struct UVersionInfo
		{
			public byte byte1;
			public byte byte2;
			public byte byte3;
			public byte byte4;
		}

		public class BrowserICUSymbols
		{
			// These methods are supplied by our own unoicu.a static library and includes support for the BreakIterator
			// API. The dotnet runtime version of ICU complains about mising resources when calling ubrk_open even
			// when udata_setCommonData is called.
			[DllImport("unoicu")]
			public static extern IntPtr uno_udata_setCommonData(IntPtr bytes);

			[DllImport("unoicu", CharSet = CharSet.Unicode)]
			public static extern IntPtr init_line_breaker(string bytes);

			[DllImport("unoicu")]
			public static extern int next_line_breaking_opportunity(IntPtr breaker);

			// These are methods from ICU that are redeclared under the uno_ prefix for WASM linking purposes.
			// These methods are also present in the dotnet runtime ICU build (through __Internal), except that
			// the symbols are not available on NativeAOT.
			[DllImport("unoicu")]
			static extern IntPtr uno_ubidi_open();

			[DllImport("unoicu")]
			static extern void uno_ubidi_close(IntPtr pBiDi);

			[DllImport("unoicu")]
			static extern void uno_ubidi_setPara(IntPtr pBiDi, IntPtr text, int length, byte paraLevel, IntPtr embeddingLevels, out int errorCode);

			[DllImport("unoicu")]
			static extern void uno_ubidi_getLogicalRun(IntPtr pBiDi, int logicalPosition, out int logicalLimit, out byte level);

			[DllImport("unoicu")]
			static extern int uno_ubidi_countRuns(IntPtr pBiDI, out int errorCode);

			[DllImport("unoicu")]
			static extern int uno_ubidi_getVisualRun(IntPtr pBiDi, int runIndex, out int logicalStart, out int length);

			[DllImport("unoicu")]
			static extern void uno_u_getVersion(out UVersionInfo versionInfo);

			[DllImport("unoicu")]
			static extern void uno_u_versionToString(IntPtr versionArray, IntPtr versionString);
		}

		private static class IOSICUSymbols
		{
			[DllImport("__Internal")]
			static extern IntPtr ubidi_open_77();

			[DllImport("__Internal")]
			static extern void ubidi_close_77(IntPtr pBiDi);

			[DllImport("__Internal")]
			static extern void ubidi_setPara_77(IntPtr pBiDi, IntPtr text, int length, byte paraLevel, IntPtr embeddingLevels, out int errorCode);

			[DllImport("__Internal")]
			static extern void ubidi_getLogicalRun_77(IntPtr pBiDi, int logicalPosition, out int logicalLimit, out byte level);

			[DllImport("__Internal")]
			static extern int ubidi_countRuns_77(IntPtr pBiDI, out int errorCode);

			[DllImport("__Internal")]
			static extern int ubidi_getVisualRun_77(IntPtr pBiDi, int runIndex, out int logicalStart, out int length);

			[DllImport("__Internal")]
			static extern IntPtr ubrk_open_77(int type, IntPtr locale, IntPtr text, int textLength, out int status);

			[DllImport("__Internal")]
			static extern IntPtr ubrk_close_77(IntPtr bi);

			[DllImport("__Internal")]
			static extern int ubrk_first_77(IntPtr bi);

			[DllImport("__Internal")]
			static extern int ubrk_next_77(IntPtr bi);

			[DllImport("__Internal")]
			static extern void u_getVersion_77(out UVersionInfo versionInfo);

			[DllImport("__Internal")]
			static extern void u_versionToString_77(IntPtr versionArray, IntPtr versionString);
		}
	}
}
