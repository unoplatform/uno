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
		// In MT mode, the runtime's JSSynchronizationContext is installed on the deputy thread.
		// Uno dispatcher work items are posted into it via Post() so the runtime's pump loop
		// drains both JS interop and Uno dispatcher items from a single queue.
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
				if (IsThreadingSupported)
				{
					_deputySynchronizationContext.Post(static _ => DispatchItems(), null);
				}
				else
				{
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
