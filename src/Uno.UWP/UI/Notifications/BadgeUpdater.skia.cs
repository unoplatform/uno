#nullable enable

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

		partial void SetBadge(string? value) => _badgeUpdaterExtension?.SetBadge(value);
	}
}
