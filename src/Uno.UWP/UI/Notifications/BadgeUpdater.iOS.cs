#nullable enable

using System.Globalization;
using UIKit;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		partial void SetBadge(string? value)
		{
			if (value != null && int.TryParse(value, CultureInfo.InvariantCulture, out var badgeNumber))
			{
				UIApplication.SharedApplication.ApplicationIconBadgeNumber = badgeNumber;
			}
			else
			{
				Clear();
			}
		}
	}
}
