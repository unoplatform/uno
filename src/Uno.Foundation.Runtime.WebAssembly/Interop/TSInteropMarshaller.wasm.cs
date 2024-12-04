#nullable enable
// #define TRACE_MEMORY_LAYOUT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Foundation;
using Uno.Foundation.Logging;

namespace Uno.Foundation.Interop
{
	internal static partial class TSInteropMarshaller
	{
		private static readonly Logger _logger = typeof(TSInteropMarshaller).Log();

		public const UnmanagedType LPUTF8Str = (UnmanagedType)48;

		public static void InvokeJS(
			string methodName,
			object paramStruct,
			[System.Runtime.CompilerServices.CallerMemberName] string? memberName = null
		)
		{
			var paramStructType = paramStruct.GetType();
			var paramSize = Marshal.SizeOf(paramStruct);

			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.Trace($"InvokeJS for {memberName}/{paramStructType} (Alloc: {paramSize})");
			}

			var pParms = Marshal.AllocHGlobal(paramSize);

			DumpStructureLayout(paramStructType);

			Marshal.StructureToPtr(paramStruct, pParms, false);

			WebAssemblyRuntime.InvokeJSUnmarshalled(methodName, pParms, out var exception);

			Marshal.DestroyStructure(pParms, paramStructType);
			Marshal.FreeHGlobal(pParms);

			if (exception != null)
			{
				if (_logger.IsEnabled(LogLevel.Error))
				{
					_logger.Error($"Failed InvokeJS for {memberName}/{paramStructType}: {exception}");
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

			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.Trace($"InvokeJS for {memberName}/{paramStructType}/{retStructType} (paramSize: {paramSize}, returnSize: {returnSize}");
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
				if (_logger.IsEnabled(LogLevel.Error))
				{
					_logger.Debug($"Failed InvokeJS for {memberName}/{paramStructType}: {e}");
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

		/// <summary>
		/// Allocates a shared instance of <typeparamref name="T"/> between JavaScript and managed code.
		/// </summary>
		/// <typeparam name="T">Type of the shared instance.</typeparam>
		/// <param name="propertySetterName">
		/// Javascript method name to invoke to "set" the pointer to the marshaled instance.
		/// The method must accepts a single number argument which is the pointer.
		/// </param>
		/// <param name="propertyResetName">
		/// Javascript method name to invoke to "unset" the pointer to the marshaled instance.
		/// This will be invoked when the resulting <see cref="HandleRef{T}"/> is being disposed.
		/// The method must accepts a single number argument which is the pointer.
		/// </param>
		/// <remarks>
		/// <paramref name="propertySetterName"/> and <paramref name="propertyResetName"/> methods must use the <see cref="InvokeJS(string,object,string?)"/> syntax.
		/// (I.e. no direct javascript code!)
		/// </remarks>
		/// <returns>A reference to the shared instance.</returns>
		public static HandleRef<T> Allocate<T>(string propertySetterName, string? propertyResetName = null)
			where T : struct
		{
			var value = new HandleRef<T>(propertyResetName);
			try
			{
				WebAssemblyRuntime.InvokeJSUnmarshalled(propertySetterName, value.Handle);
			}
			catch (Exception e)
			{
				try
				{
					value.Dispose();
				}
				// If the allocation failed, the dispose will most likely also fail,
				// but we want to propagate the real exception of the allocation!
				catch (Exception) { }

				if (_logger.IsEnabled(LogLevel.Error))
				{
					_logger.Debug($"Failed Allocate {propertySetterName}/{value.Type}: {e}");
				}

				throw;
			}

			return value;
		}

		public static AutoPtr AllocateBlittableStructure(Type type)
		{
			var size = Marshal.SizeOf(type);

			return new AutoPtr(Marshal.AllocHGlobal(size));
		}

		public static StructPtr AllocateStructure(Type type)
		{
			var size = Marshal.SizeOf(type);

			return new StructPtr(Marshal.AllocHGlobal(size), type);
		}

		public static T UnmarshalStructure<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(AutoPtr ptr)
			where T : struct
		{
			return Marshal.PtrToStructure<T>(ptr);
		}

		public static T UnmarshalStructure<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(StructPtr ptr)
			where T : struct
		{
			return Marshal.PtrToStructure<T>(ptr);
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
