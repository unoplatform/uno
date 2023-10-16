using System;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		private bool GetHasThreadAccess() => throw new NotSupportedException("Ref assembly");
	}
}
