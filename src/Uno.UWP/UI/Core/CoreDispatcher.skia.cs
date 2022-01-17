using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{		
		static readonly IDictionary<CoreDispatcherPriority, global::System.Threading.SynchronizationContext> contexCache
			= new Dictionary<CoreDispatcherPriority, global::System.Threading.SynchronizationContext>(5);
		/// <summary>
		/// Provide a action that will delegate the dispach of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Action<Action> DispatchOverride
		{
			get => Uno.UI.Dispatching.CoreDispatcher.DispatchOverride;
			set => Uno.UI.Dispatching.CoreDispatcher.DispatchOverride = value;
		}

		/// <summary>
		/// Provide a action that will delegate the dispach of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Func<bool> HasThreadAccessOverride
		{
			get => Uno.UI.Dispatching.CoreDispatcher.HasThreadAccessOverride;
			set => Uno.UI.Dispatching.CoreDispatcher.HasThreadAccessOverride = value;
		}

		[global::System.Runtime.CompilerServices
			.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		internal global::System.Threading.SynchronizationContext GetSynchronizationContextFromPriority(CoreDispatcherPriority priority)
		{
			if (!contexCache.TryGetValue(priority,out var ctx))
			{
				contexCache[priority] = ctx = new CoreDispatcherSynchronizationContext(_inner.GetSynchronizationContextFromPriority((Uno.UI.Dispatching.CoreDispatcherPriority)priority));
			}
			return ctx;
		}
	}
}
