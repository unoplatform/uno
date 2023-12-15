using System;
using System.ComponentModel;

using Uno.UI.Dispatching;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		/// <summary>
		/// Provide a action that will delegate the dispach of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Action<Action, NativeDispatcherPriority> DispatchOverride
		{
			get => NativeDispatcher.DispatchOverride;
			set => NativeDispatcher.DispatchOverride = value;
		}

		/// <summary>
		/// Provide a action that will delegate the dispach of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Func<bool> HasThreadAccessOverride
		{
			get => NativeDispatcher.HasThreadAccessOverride;
			set => NativeDispatcher.HasThreadAccessOverride = value;
		}
	}
}
