#nullable enable

using Uno.Foundation;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.UI.Notifications.BadgeUpdater.NativeMethods;
#endif

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.UI.Notifications.BadgeUpdater";
#endif

		partial void SetBadge(string? value)
		{
			if (int.TryParse(value, out var number))
			{
#if NET7_0_OR_GREATER
				NativeMethods.SetNumber(number);
#else
				WebAssemblyRuntime.InvokeJS($"{JsType}.setNumber({number})");
#endif
			}
			else
			{
#if NET7_0_OR_GREATER
				NativeMethods.Clear();
#else
				WebAssemblyRuntime.InvokeJS($"{JsType}.clear()");
#endif
			}
		}
	}
}
