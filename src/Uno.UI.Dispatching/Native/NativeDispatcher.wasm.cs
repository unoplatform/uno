using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;

using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		// In MT mode, the runtime installs its JSSynchronizationContext on the deputy thread, captured
		// here since Application.Start replaces it with Uno's. Other threads post dispatcher work into it.
		private static SynchronizationContext _deputySynchronizationContext;

		private static int _uiThreadId;

		[JSExport]
		private static void DispatcherCallback()
		{
			if (typeof(NativeDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(NativeDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: NativeDispatcher.DispatcherCallback()");
			}

			DispatchItems();
		}

		[JSExport]
		private static Task DispatcherCallbackAsync()
		{
			DispatcherCallback();

			return Task.CompletedTask;
		}

		partial void Initialize()
		{
			_uiThreadId = Environment.CurrentManagedThreadId;

			IsThreadingSupported = WebAssemblyThreading.IsThreadingEnabled;

			if (typeof(NativeDispatcher).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(NativeDispatcher).Log().Trace($"[tid:{Environment.CurrentManagedThreadId}]: NativeDispatcher.Initialize() IsThreadingSupported:{IsThreadingSupported}");
			}

			if (IsThreadingSupported)
			{
				// Capture the runtime's JSSynchronizationContext before any app code runs.
				_deputySynchronizationContext = System.Threading.SynchronizationContext.Current;
			}
		}

		internal static bool IsThreadingSupported { get; private set; }

		private bool GetHasThreadAccess()
			=> !IsThreadingSupported || Environment.CurrentManagedThreadId == _uiThreadId;

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
				if (IsThreadingSupported && Environment.CurrentManagedThreadId != _uiThreadId)
				{
					_deputySynchronizationContext.Post(static _ => DispatchItems(), null);
				}
				else
				{
					// Always through the event loop, even in MT: the runtime's pump drains its queue until empty before
					// yielding, so an item posting the next one would chain forever and starve everything the event
					// loop delivers (JS calls into managed, timers, animation frames).
					NativeMethods.WakeUp();
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
