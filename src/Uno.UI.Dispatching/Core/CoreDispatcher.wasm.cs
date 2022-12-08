using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching
{
	internal sealed partial class CoreDispatcher
	{
		internal static bool IsThreadingSupported { get; }
			= Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_FEATURES")
				?.Split(',').Any(v => v.Equals("threads", StringComparison.OrdinalIgnoreCase)) ?? false;

		/// <summary>
		/// Method invoked from 
		/// </summary>
		private static int DispatcherCallback()
		{
			if (typeof(CoreDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(CoreDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: CoreDispatcher.DispatcherCallback()");
			}

			Main.DispatchItems();
			return 0; // Required by bind_static_method (void is not supported)
		}

		/// <summary>
		/// Provide a action that will delegate the dispatch of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Action<Action> DispatchOverride;

		partial void Initialize()
		{
			if (typeof(CoreDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(CoreDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: CoreDispatcher.Initialize IsThreadingSupported:{IsThreadingSupported}");
			}

			if (IsThreadingSupported && Environment.CurrentManagedThreadId != 1)
			{
				throw new InvalidOperationException($"CoreDispatcher must be initialized on the main Javascript thread");
			}
		}

		// Always reschedule, otherwise we may end up in live-lock.
		public static bool HasThreadAccessOverride { get; set; }

		private bool GetHasThreadAccess()
			=> IsThreadingSupported ? Environment.CurrentManagedThreadId == 1 : HasThreadAccessOverride;

		partial void EnqueueNative()
		{
			if (typeof(CoreDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(CoreDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: CoreDispatcher.EnqueueNative()");
			}

			if (DispatchOverride == null)
			{
				if (!IsThreadingSupported || (IsThreadingSupported && GetHasThreadAccess()))
				{
					WebAssemblyRuntime.InvokeJSUnmarshalled("CoreDispatcher:WakeUp", IntPtr.Zero);
				}
				else
				{
					// This is a separate function to avoid enclosing early resolution
					// by the interpreter/JIT, in case we're running the non-threading
					// enabled runtime.
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
	}
}
