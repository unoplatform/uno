using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Foundation.Logging;
using System.Runtime.CompilerServices;

namespace Windows.UI.Core
{
	public delegate void DispatchedHandler();
	public delegate void CancellableDispatchedHandler(CancellationToken ct);
	public delegate void IdleDispatchedHandler(IdleDispatchedHandlerArgs e);

	/// <summary>
	/// Defines a priority-based UI Thread scheduler.
	/// </summary>
	/// <remarks>
	/// This implementation is based on the fact that the native queue will 
	/// only contain one instance of the callback for the current core dispatcher.
	/// 
	/// This gives the native events, such as touch, the priority over managed-side queued
	/// events, and will allow a properly prioritized processing of idle events.
	/// </remarks>
	public sealed partial class CoreDispatcher
	{
		private Uno.UI.Dispatching.CoreDispatcher _inner = Uno.UI.Dispatching.CoreDispatcher.Main;

		/// <summary>
		/// Gets the dispatcher for the main thread.
		/// </summary>
		internal static CoreDispatcher Main { get; } = new CoreDispatcher();

		public CoreDispatcher()
		{
		}

		/// <summary>
		/// Enforce access on the UI thread.
		/// </summary>
		internal static void CheckThreadAccess()
		{
#if !__WASM__
			// This check is disabled on WASM until threading support is enabled, since HasThreadAccess is currently user-configured (and defaults to false).
			if (!Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The application called an interface that was marshalled for a different thread.");
			}
#endif
		}

		/// <summary>
		/// Determines if the current thread has access to this CoreDispatcher.
		/// </summary>
		public bool HasThreadAccess
			=> _inner.HasThreadAccess;

		/// <summary>
		/// Gets the priority of the current task.
		/// </summary>
		/// <remarks>Sets has no effect on Uno</remarks>
		public CoreDispatcherPriority CurrentPriority
		{
			get => (CoreDispatcherPriority)_inner.CurrentPriority;
			[Uno.NotImplemented] set { } // Drop the set done by external code
		}

		/// <summary>
		/// Schedules the provided handler on the dispatcher.
		/// </summary>
		/// <param name="priority">The execution priority for the handler</param>
		/// <param name="handler">The handler to execute</param>
		/// <returns>An async operation for the scheduled handler.</returns>
		public IAsyncAction RunAsync(CoreDispatcherPriority priority, DispatchedHandler handler)
			=> _inner.RunAsync((Uno.UI.Dispatching.CoreDispatcherPriority)priority, new Uno.UI.Dispatching.DispatchedHandler(handler));

		/// <summary>
		/// Schedules the provided handler using the idle priority
		/// </summary>
		/// <param name="handler">The handler to execute</param>
		/// <returns>An async operation for the scheduled handler.</returns>
		public IAsyncAction RunIdleAsync(IdleDispatchedHandler handler)
			=> _inner.RunIdleAsync(c => handler(new IdleDispatchedHandlerArgs(c)));

#if __ANDROID__
		internal Uno.UI.Dispatching.UIAsyncOperation RunAnimation(DispatchedHandler handler)
		{
			return _inner.RunAnimation(new Uno.UI.Dispatching.DispatchedHandler(handler));
		}
#endif
	}
}
