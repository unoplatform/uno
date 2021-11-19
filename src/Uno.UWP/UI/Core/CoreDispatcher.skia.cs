using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
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
	}
}
