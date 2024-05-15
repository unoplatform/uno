#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using Uno.UI.Dispatching;
using Windows.Foundation;

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
		private NativeDispatcher _inner = NativeDispatcher.Main;

		/// <summary>
		/// Gets the dispatcher for the main thread.
		/// </summary>
		internal static CoreDispatcher Main { get; } = new CoreDispatcher();

		private CoreDispatcher()
		{
			Debug.Assert(
				(int)CoreDispatcherPriority.High == 1 &&
				(int)CoreDispatcherPriority.Normal == 0 &&
				(int)CoreDispatcherPriority.Low == -1 &&
				(int)CoreDispatcherPriority.Idle == -2 &&
				Enum.GetValues<CoreDispatcherPriority>().Length == 4);
		}

		/// <summary>
		/// Enforce access on the UI thread.
		/// </summary>
		internal static void CheckThreadAccess() => NativeDispatcher.CheckThreadAccess();

		/// <summary>
		/// Determines if the current thread has access to this CoreDispatcher.
		/// </summary>
		public bool HasThreadAccess => _inner.HasThreadAccess;

		/// <summary>
		/// Gets the priority of the current task.
		/// </summary>
		/// <remarks>Sets has no effect on Uno</remarks>
		public CoreDispatcherPriority CurrentPriority
		{
			get => (CoreDispatcherPriority)(~_inner.CurrentPriority + 2);
			[Uno.NotImplemented]
			set { } // Drop the set done by external code
		}

		/// <summary>
		/// Schedules the provided handler on the dispatcher.
		/// </summary>
		/// <param name="priority">The execution priority for the handler</param>
		/// <param name="agileCallback">The handler to execute</param>
		/// <returns>An async operation for the scheduled handler.</returns>
		public IAsyncAction RunAsync(CoreDispatcherPriority priority, DispatchedHandler agileCallback)
			=> _inner.EnqueueOperation(Unsafe.As<Action>(agileCallback), (NativeDispatcherPriority)(~priority + 2));

		/// <summary>
		/// Schedules the provided handler using the idle priority
		/// </summary>
		/// <param name="agileCallback">The handler to execute</param>
		/// <returns>An async operation for the scheduled handler.</returns>
		public IAsyncAction RunIdleAsync(IdleDispatchedHandler agileCallback)
			=> _inner.EnqueueIdleOperation(d => agileCallback(new IdleDispatchedHandlerArgs(d)));

#if __ANDROID__
		internal UIAsyncOperation RunAnimation(Action handler)
			=> _inner.RunAnimation(handler);
#endif
	}
}
