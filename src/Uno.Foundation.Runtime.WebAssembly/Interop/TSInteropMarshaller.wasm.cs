#nullable enable
// #define TRACE_MEMORY_LAYOUT

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
	internal static class TSInteropMarshaller
	{
		private static readonly Lazy<ILogger> _logger = new Lazy<ILogger>(() => typeof(TSInteropMarshaller).Log());

		public const UnmanagedType LPUTF8Str = (UnmanagedType)48;

		public static void InvokeJS(
			string methodName,
			object paramStruct,
			[System.Runtime.CompilerServices.CallerMemberName] string? memberName = null
		)
		{
			var paramStructType = paramStruct.GetType();
			var paramSize = Marshal.SizeOf(paramStruct);

			if (_logger.Value.IsEnabled(LogLevel.Trace))
			{
				_logger.Value.LogTrace($"InvokeJS for {memberName}/{paramStructType} (Alloc: {paramSize})");
			}

			var pParms = Marshal.AllocHGlobal(paramSize);

			DumpStructureLayout(paramStructType);

			Marshal.StructureToPtr(paramStruct, pParms, false);

			WebAssemblyRuntime.InvokeJSUnmarshalled(methodName, pParms, out var exception);

			Marshal.DestroyStructure(pParms, paramStructType);
			Marshal.FreeHGlobal(pParms);

			if (exception != null)
			{
				if (_logger.Value.IsEnabled(LogLevel.Error))
				{
					_logger.Value.LogError($"Failed InvokeJS for {memberName}/{paramStructType}: {exception}");
				}

				throw exception;
			}
		}

		public static object InvokeJS(
			string methodName,
			object paramStruct,
			Type retStructType,
			[System.Runtime.CompilerServices.CallerMemberName] string? memberName = null
		)
		{
			var paramStructType = paramStruct.GetType();

			var returnSize = Marshal.SizeOf(retStructType);
			var paramSize = Marshal.SizeOf(paramStructType);

			if (_logger.Value.IsEnabled(LogLevel.Trace))
			{
				_logger.Value.LogTrace($"InvokeJS for {memberName}/{paramStructType}/{retStructType} (paramSize: {paramSize}, returnSize: {returnSize}");
			}

			DumpStructureLayout(paramStructType);
			DumpStructureLayout(retStructType);

			var pParms = Marshal.AllocHGlobal(paramSize);
			var pReturnValue = Marshal.AllocHGlobal(returnSize);

			try
			{
				Marshal.StructureToPtr(paramStruct, pParms, false);

				var ret = WebAssemblyRuntime.InvokeJSUnmarshalled(methodName, pParms, pReturnValue);

				var returnValue = Marshal.PtrToStructure(pReturnValue, retStructType);
				return returnValue!;
			}
			catch (Exception e)
			{
				if (_logger.Value.IsEnabled(LogLevel.Error))
				{
					_logger.Value.LogDebug($"Failed InvokeJS for {memberName}/{paramStructType}: {e}");
				}
				throw;
			}
			finally
			{
				Marshal.DestroyStructure(pParms, paramStructType);
				Marshal.FreeHGlobal(pParms);

				Marshal.DestroyStructure(pReturnValue, retStructType);
				Marshal.FreeHGlobal(pReturnValue);
			}
		}

#if TRACE_MEMORY_LAYOUT
		private static HashSet<Type> _structureDump = new HashSet<Type>();
#endif

		[Conditional("DEBUG")]
		private static void DumpStructureLayout(Type structType)
		{
#if TRACE_MEMORY_LAYOUT
			if (structType == typeof(bool))
			{
				return;
			}

			if (!_structureDump.Contains(structType))
			{
				_structureDump.Add(structType);

				Console.WriteLine($"Dumping offsets for {structType} (Size: {Marshal.SizeOf(structType)})");

				foreach (var field in structType.GetFields())
				{
					var offset = Marshal.OffsetOf(structType, field.Name);
					Console.WriteLine($"  - {field.Name}: {offset}");
				}
			}
#endif
		}
	}
}
