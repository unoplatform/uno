using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Extensions;
using Uno.Foundation;

namespace Uno.Foundation.Interop
{
	public static class TSInteropMarshaller 
	{
		private static readonly Lazy<ILogger> _logger = new Lazy<ILogger>(() => typeof(TSInteropMarshaller).Log());

		public const UnmanagedType LPUTF8Str = (UnmanagedType)48;

		public static void InvokeJS<TParam>(
			string methodName,
			TParam paramStruct,
			[System.Runtime.CompilerServices.CallerMemberName] string memberName = null
		)
		{
			var paramSize = MarshalSizeOf<TParam>.Size;

			if (_logger.Value.IsEnabled(LogLevel.Trace))
			{
				_logger.Value.LogTrace($"InvokeJS for {memberName}/{typeof(TParam)} (Alloc: {paramSize})");
			}

			var pParms = Marshal.AllocHGlobal(paramSize);

			DumpStructureLayout<TParam>();

			Marshal.StructureToPtr(paramStruct, pParms, false);

			WebAssemblyRuntime.InvokeJSUnmarshalled(methodName, pParms, out var exception);

			Marshal.DestroyStructure(pParms, typeof(TParam));
			Marshal.FreeHGlobal(pParms);

			if (exception != null)
			{
				if (_logger.Value.IsEnabled(LogLevel.Error))
				{
					_logger.Value.LogError($"Failed InvokeJS for {memberName}/{typeof(TParam)}: {exception}");
				}

				throw exception;
			}
		}

		public static TRet InvokeJS<TParam, TRet>(
			string methodName,
			TParam paramStruct,
			[System.Runtime.CompilerServices.CallerMemberName] string memberName = null
		)
		{
			var returnSize = MarshalSizeOf<TRet>.Size;
			var paramSize = MarshalSizeOf<TParam>.Size;

			if (_logger.Value.IsEnabled(LogLevel.Trace))
			{
				_logger.Value.LogTrace($"InvokeJS for {memberName}/{typeof(TParam)}/{typeof(TRet)} (paramSize: {paramSize}, returnSize: {returnSize}");
			}

			DumpStructureLayout<TParam>();
			DumpStructureLayout<TRet>();

			var pParms = Marshal.AllocHGlobal(paramSize);
			var pReturnValue = Marshal.AllocHGlobal(returnSize);

			TRet returnValue = default;

			try
			{
				Marshal.StructureToPtr(paramStruct, pParms, false);
				Marshal.StructureToPtr(returnValue, pReturnValue, false);

				var ret = WebAssemblyRuntime.InvokeJSUnmarshalled(methodName, pParms, pReturnValue);

				returnValue = (TRet)Marshal.PtrToStructure(pReturnValue, typeof(TRet));
				return returnValue;
			}
			catch (Exception e)
			{
				if (_logger.Value.IsEnabled(LogLevel.Error))
				{
					_logger.Value.LogDebug($"Failed InvokeJS for {memberName}/{typeof(TParam)}: {e}");
				}
				throw;
			}
			finally
			{
				Marshal.DestroyStructure(pParms, typeof(TParam));
				Marshal.FreeHGlobal(pParms);

				Marshal.DestroyStructure(pReturnValue, typeof(TRet));
				Marshal.FreeHGlobal(pReturnValue);
			}
		}

#if TRACE_MEMORY_LAYOUT
		private static HashSet<Type> _structureDump = new HashSet<Type>();
#endif

		[Conditional("DEBUG")]
		private static void DumpStructureLayout<T>()
		{
#if TRACE_MEMORY_LAYOUT
			if (typeof(T) == typeof(bool))
			{
				return;
			}

			if (!_structureDump.Contains(typeof(T)))
			{
				_structureDump.Add(typeof(T));

				Console.WriteLine($"Dumping offsets for {typeof(T)} (Size: {MarshalSizeOf<T>.Size})");

				foreach (var field in typeof(T).GetFields())
				{
					var offset = Marshal.OffsetOf<T>(field.Name);
					Console.WriteLine($"  - {field.Name}: {offset}");
				}
			}
#endif
		}

		private class MarshalSizeOf<T>
		{
			internal static readonly int Size = Marshal.SizeOf(typeof(T));
		}
	}
}
