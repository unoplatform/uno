using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Foundation.Interop;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		/// <summary>
		/// Method invoked from 
		/// </summary>
		private static int DispatcherCallback()
		{
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
		}

		// Always reschedule, otherwise we may end up in live-lock.
		public static bool HasThreadAccessOverride { get; set; } = false;
		 
		private bool GetHasThreadAccess() => HasThreadAccessOverride;

		public static CoreDispatcher Main { get; } = new CoreDispatcher();

		partial void EnqueueNative()
		{
			if (DispatchOverride == null)
			{
				WebAssemblyRuntime.InvokeJSUnmarshalled("CoreDispatcher:WakeUp", IntPtr.Zero);
			}
			else
			{
				DispatchOverride(() => DispatchItems());
			}
		}
	}
}
