#nullable enable

using Uno.Foundation;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.UI.Notifications.BadgeUpdater.NativeMethods;
#endif

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		private const string JsType = "Windows.UI.Notifications.BadgeUpdater";

		partial void SetBadge(string? value)
		{
			if (int.TryParse(value, out var number))
			{
				WebAssemblyRuntime.InvokeJS($"{JsType}.setNumber({number})");
			}
			else
			{
				WebAssemblyRuntime.InvokeJS($"{JsType}.clear()");
			}
		}
	}
}
