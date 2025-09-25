#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Uno;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Documents;

internal readonly partial struct UnicodeText
{
	private static class ICU
	{
		// The version number of ICU is important because the exported symbols have there names appended by
		// the version number. For example, there's a ubrk_open_74 in ICU v74, but not a ubrl_open.
		private static readonly int _icuVersion;
		private static Func<Type, object> _getMethodMemoized;

		static unsafe ICU()
		{
			IntPtr libicuuc;
			if (OperatingSystem.IsWindows())
			{
				// On Windows, We get the ICU binaries from the Microsoft.ICU.ICU4C.Runtime package
				_icuVersion = 72;
				if (NativeLibrary.TryLoad("icuuc72", typeof(ICU).Assembly, DllImportSearchPath.UserDirectories, out var handle))
				{
					libicuuc = handle;
				}
				else
				{
					throw new Exception("Failed to load libicuuc.");
				}
			}
			else if (OperatingSystem.IsLinux())
			{
				// On Linux, we get the ICU binaries from the dynamic linker search path (usually /usr/lib64/)
				if (NativeLibrary.TryLoad("icuuc", typeof(ICU).Assembly, DllImportSearchPath.UserDirectories, out var handle))
				{
					libicuuc = handle;
				}
				else
				{
					throw new Exception("Failed to load libicuuc.");
				}

				// Since libicuuc not installed by us, we have no control over the specific version number, so
				// we try a wide range of versions.
				for (int i = 100; i >= 67; i--)
				{
					if (NativeLibrary.TryGetExport(libicuuc, $"u_getVersion_{i}", out var getVersion))
					{
						_icuVersion = i;
					}
				}
			}
			else
			{
				throw new DllNotFoundException("Failed to load libicuuc.");
			}

			_getMethodMemoized = Funcs.CreateMemoized<Type, object>(type =>
			{
				if (NativeLibrary.TryGetExport(libicuuc, type.Name, out var func))
				{
					return Marshal.GetDelegateForFunctionPointer(func, type);
				}
				if (NativeLibrary.TryGetExport(libicuuc, $"{type.Name}_{_icuVersion}", out var func2))
				{
					return Marshal.GetDelegateForFunctionPointer(func2, type);
				}
				throw new Exception($"Failed to obtain the {type.Name} method from the ICU libraries.");
			});

			GetMethod<u_getVersion>()(out var versionInfo);
			var ptr = Marshal.AllocHGlobal(1000);
			GetMethod<u_versionToString>()((IntPtr)(&versionInfo), ptr);
			typeof(ICU).LogInfo()?.Info($"Found ICU version {Marshal.PtrToStringAnsi(ptr)}.");
			Marshal.FreeHGlobal(ptr);
		}

		public static T GetMethod<T>() => (T)_getMethodMemoized(typeof(T));

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
				// What ICU has a very low bar for what it considers a "warning", so this can be very spammy. 
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
		internal delegate int ubidi_getVisualRun(IntPtr pBiDi, int runIndex, out int logicalStart, out int length);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate IntPtr ubrk_open(int type, IntPtr locale, IntPtr text, int textLength, out int status);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate IntPtr ubrk_close(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int ubrk_first(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int ubrk_next(IntPtr bi);

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
	}
}
