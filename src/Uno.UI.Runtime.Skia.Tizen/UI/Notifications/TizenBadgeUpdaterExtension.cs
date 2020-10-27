#nullable enable

using Tizen.Applications;
using Uno.UI.Notifications;

namespace Uno.UI.Runtime.Skia.Tizen.UI.Notifications
{
	internal class TizenBadgeUpdaterExtension : IBadgeUpdaterExtension
	{
		public TizenBadgeUpdaterExtension(object owner)
		{
		}

		public void SetBadge(string? value)
		{
			var appId = Application.Current.ApplicationInfo.ApplicationId;

			var existingBadge = BadgeControl.Find(appId);
			if (existingBadge != null || value == null || !int.TryParse(value, out var number))
			{
				BadgeControl.Remove(appId);
				return;
			}

			if (value != null)
			{
				Badge badge = new Badge(appId, number);
				BadgeControl.Add(badge);
			}
		}
	}
}
