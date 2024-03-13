using System;
using System.ComponentModel;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Action<Action> DispatchOverride;

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static Func<bool> HasThreadAccessOverride;

		private bool GetHasThreadAccess() => throw new NotSupportedException("Ref assembly");
	}
}
