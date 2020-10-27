#nullable enable

using System;
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

			try
			{
				BadgeControl.Remove(appId);
			}
			catch (InvalidOperationException)
			{
				// This exception is thrown when the badge does not exist.
				// Seems there is no way to check if the badge exists.
			}
			if (value == null || !int.TryParse(value, out var number))
			{
				return;
			}

			Badge badge = new Badge(appId, number);
			BadgeControl.Add(badge);
		}
	}
}
