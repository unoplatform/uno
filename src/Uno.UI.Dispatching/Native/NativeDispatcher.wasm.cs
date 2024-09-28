using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
#if NET9_0_OR_GREATER
		private static SynchronizationContext _jsContext = SynchronizationContext.Current;
#endif

#pragma warning disable IDE0051 // Remove unused private members
		[JSExport]
		private static void DispatcherCallback()
#pragma warning restore IDE0051 // Remove unused private members
		{
			if (typeof(NativeDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(NativeDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: NativeDispatcher.DispatcherCallback()");
			}

			DispatchItems();
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

		private bool GetHasThreadAccess()
			=> !IsThreadingSupported || Environment.CurrentManagedThreadId == 1;

		/// <summary>
		/// Provide an action that will delegate the dispatch of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Action<Action, NativeDispatcherPriority> DispatchOverride;

		partial void EnqueueNative(NativeDispatcherPriority priority)
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
#if NET9_0_OR_GREATER
					_jsContext.Post(_ => DispatcherCallback(), null);
#else
					// This is a separate function to avoid enclosing early resolution
					// by the interpreter/JIT, in case we're running the non-threaded
					// runtime.
					static void InvokeOnMainThread()
						=> WebAssembly.JSInterop.InternalCalls.InvokeOnMainThread();

					InvokeOnMainThread();
#endif
				}
			}
			else
			{
				DispatchOverride(NativeDispatcher.DispatchItems, priority);
			}
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Uno.UI.Dispatching.NativeDispatcher.WakeUp")]
			internal static partial void WakeUp();
		}
	}
}
