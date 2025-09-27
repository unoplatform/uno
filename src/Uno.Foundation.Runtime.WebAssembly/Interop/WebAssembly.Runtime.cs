#nullable enable

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation.Runtime.WebAssembly.Interop;

namespace WebAssembly
{
	[Obfuscation(Feature = "renaming", Exclude = true)]
	internal sealed partial class Runtime
	{
		/// <summary>
		/// Invokes Javascript code in the hosting environment
		/// </summary>
		[JSImport("globalThis.Uno.UI.Interop.Runtime.InvokeJS")]
		internal static partial string InvokeJS(string value);

		[JSImport("globalThis.MonoSupport.jsCallDispatcher.invokeJSUnmarshalled")]
		internal static partial IntPtr InvokeJSUnmarshalled(string? functionIdentifier, IntPtr arg0, IntPtr arg1, IntPtr arg2);
	}

	namespace JSInterop
	{
		internal static class InternalCalls
		{
#if NET9_0_OR_GREATER
			public static void InvokeOnMainThread()
			{
				throw new NotSupportedException($"Uno Platform on net10.0 does not support threading yet.");
			}
#else
			// Uno-Specific implementation for https://github.com/dotnet/runtime/issues/69409.
			// To be removed when the runtime will support the main SynchronizationContext.
			[MethodImplAttribute(MethodImplOptions.InternalCall)]
			public static extern void InvokeOnMainThread();
#endif
		}
	}
}
