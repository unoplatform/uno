using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;

using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
#pragma warning disable IDE0051 // Remove unused private members
		[JSExport]
		private static void DispatcherCallback()
#pragma warning restore IDE0051 // Remove unused private members
		{
			if (typeof(NativeDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(NativeDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: NativeDispatcher.DispatcherCallback()");
			}

			Main.DispatchItems();
		}

		partial void Initialize()
		{
			if (typeof(NativeDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(NativeDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: NativeDispatcher.Initialize() IsThreadingSupported:{IsThreadingSupported}");
			}

			if (IsThreadingSupported && Environment.CurrentManagedThreadId != 1)
			{
				throw new InvalidOperationException($"NativeDispatcher must be initialized on the main thread.");
			}
		}

		internal static bool IsThreadingSupported { get; }
			= Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_FEATURES")
				?.Split(',').Contains("threads", StringComparer.OrdinalIgnoreCase) ?? false;

		// Always reschedule, otherwise we may end up in live-lock.
		internal static bool HasThreadAccessOverride { get; set; }

		private bool GetHasThreadAccess()
			=> IsThreadingSupported ? Environment.CurrentManagedThreadId == 1 : HasThreadAccessOverride;

		/// <summary>
		/// Provide an action that will delegate the dispatch of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Action<Action> DispatchOverride;

		partial void EnqueueNative()
		{
			if (typeof(NativeDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(NativeDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: NativeDispatcher.EnqueueNative()");
			}

			if (DispatchOverride == null)
			{
				if (!IsThreadingSupported || Environment.CurrentManagedThreadId == 1)
				{
					NativeMethods.WakeUp();
				}
				else
				{
					// This is a separate function to avoid enclosing early resolution
					// by the interpreter/JIT, in case we're running the non-threaded
					// runtime.
					static void InvokeOnMainThread()
						=> WebAssembly.JSInterop.InternalCalls.InvokeOnMainThread();

					InvokeOnMainThread();
				}
			}
			else
			{
				DispatchOverride(() => DispatchItems());
			}
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Uno.UI.Dispatching.NativeDispatcher.WakeUp")]
			internal static partial void WakeUp();
		}
	}
}
