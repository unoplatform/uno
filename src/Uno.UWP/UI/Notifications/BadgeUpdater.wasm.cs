#nullable enable

using System.Globalization;
using Uno.Foundation;

using NativeMethods = __Windows.UI.Notifications.BadgeUpdater.NativeMethods;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		partial void SetBadge(string? value)
		{
			if (int.TryParse(value, CultureInfo.InvariantCulture, out var number))
			{
				NativeMethods.SetNumber(number);
			}
			else
			{
				NativeMethods.Clear();
			}
		}
	}
}
