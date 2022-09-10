using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uno.Foundation.Runtime.WebAssembly.Interop;

namespace WebAssembly
{
	[Obfuscation(Feature = "renaming", Exclude = true)]
	internal sealed class Runtime
	{
		internal static bool RethrowNativeExceptions { get; set; } = true;

		/// <summary>
		/// Mono specific internal call.
		/// </summary>
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern string InvokeJS(string str, out int exceptional_result);

		// Disable inlining to avoid the interpreter to evaluate an internal call that may not be available
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static string MonoInvokeJS(string str, out int exceptionResult) => InvokeJS(str, out exceptionResult);

		// Disable inlining to avoid the interpreter to evaluate an internal call that may not be available
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static string NetCoreInvokeJS(string str, out int exceptionResult)
			=> Interop.Runtime.InvokeJS(str, out exceptionResult);

		/// <summary>
		/// Invokes Javascript code in the hosting environment
		/// </summary>
		internal static string InvokeJS(string str)
		{
			int exceptionResult;
			var result = PlatformHelper.IsNetCore
				? NetCoreInvokeJS(str, out exceptionResult)
				: MonoInvokeJS(str, out exceptionResult);

			if (exceptionResult != 0)
			{
				var errorMessage = $"Error #{exceptionResult} \"{result}\" executing javascript: \"{str}\"";
				if (RethrowNativeExceptions)
				{
					throw new InvalidOperationException(errorMessage);
				}
				else
				{
					Console.Error.WriteLine(errorMessage);
				}
			}
			return result;
		}
	}

	namespace JSInterop
	{
		internal static class InternalCalls
		{
			// Matches this signature:
			// https://github.com/mono/mono/blob/f24d652d567c4611f9b4e3095be4e2a1a2ab23a4/sdks/wasm/driver.c#L21
			[MethodImpl(MethodImplOptions.InternalCall)]
			[EditorBrowsable(EditorBrowsableState.Never)]
			public static extern IntPtr InvokeJSUnmarshalled(out string exceptionMessage, string functionIdentifier, IntPtr arg0, IntPtr arg1, IntPtr arg2);

			// Uno-Specific implementation for https://github.com/dotnet/runtime/issues/69409.
			// To be removed when the runtime will support the main SynchronizationContext.
			[MethodImplAttribute(MethodImplOptions.InternalCall)]
			public static extern void InvokeOnMainThread();
		}
	}
}
