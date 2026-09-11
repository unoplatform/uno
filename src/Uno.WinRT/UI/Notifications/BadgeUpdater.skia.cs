#nullable enable

using System.Globalization;
using Uno.Foundation.Extensibility;
using Uno.UI.Notifications;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		private IBadgeUpdaterExtension? _badgeUpdaterExtension;

		partial void InitPlatform()
		{
			ApiExtensibility.CreateInstance(this, out _badgeUpdaterExtension);
		}

		partial void SetBadge(string? value)
		{
			if (int.TryParse(value, CultureInfo.InvariantCulture, out var numericValue))
			{
				_badgeUpdaterExtension?.SetBadge(numericValue);
			}
			else
			{
				_badgeUpdaterExtension?.SetBadge(null);
			}
		}
	}
}
