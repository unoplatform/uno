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
		private static readonly int _icuVersion;
		private static Func<Type, object> _getMethodMemoized;

		static ICU()
		{
			var openLibraries = new List<IntPtr>();
			if (OperatingSystem.IsWindows())
			{
				_icuVersion = 72;
				foreach (var libraryName in (ReadOnlySpan<string>)["icudt72", "icuin72", "icuuc72"])
				{
					if (NativeLibrary.TryLoad(libraryName, typeof(ICU).Assembly, DllImportSearchPath.UserDirectories, out var handle))
					{
						openLibraries.Add(handle);
					}
					else
					{
						typeof(ICU).LogError()?.Error($"Failed to load the {libraryName} library.");
					}
				}
			}

			_getMethodMemoized = Funcs.CreateMemoized<Type, object>(type =>
			{
				foreach (var handle in openLibraries)
				{
					if (NativeLibrary.TryGetExport(handle, type.Name, out var func))
					{
						return Marshal.GetDelegateForFunctionPointer(func, type);
					}
					if (NativeLibrary.TryGetExport(handle, $"{type.Name}_{_icuVersion}", out var func2))
					{
						return Marshal.GetDelegateForFunctionPointer(func2, type);
					}
				}
				throw new Exception($"Failed to obtain the {type.Name} method from the ICU libraries.");
			});
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
	}
}
