#nullable disable

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
		internal bool IsThreadingSupported { get; } = Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_CONFIGURATION").StartsWith("threads", StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Provide a action that will delegate the dispatch of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Action<Action> DispatchOverride
		{
			get => Uno.UI.Dispatching.CoreDispatcher.DispatchOverride;
			set => Uno.UI.Dispatching.CoreDispatcher.DispatchOverride = value;
		}

		// Always reschedule, otherwise we may end up in live-lock.
		public static bool HasThreadAccessOverride => Uno.UI.Dispatching.CoreDispatcher.HasThreadAccessOverride;
	}
}
