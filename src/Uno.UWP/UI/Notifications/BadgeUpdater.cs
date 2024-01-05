#if !__ANDROID__

using System;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		private const string BadgeNodeXPath = "/badge";
		private const string ValueAttribute = "value";

		internal BadgeUpdater()
		{
			InitPlatform();
		}

		partial void InitPlatform();

		public void Update(BadgeNotification notification)
		{
			if (notification is null)
			{
				throw new ArgumentNullException(nameof(notification));
			}

			var element = notification.Content.SelectSingleNode(BadgeNodeXPath) as XmlElement;
			var attributeValue = element?.GetAttribute(ValueAttribute);
			SetBadge(attributeValue);
		}

		public void Clear() => SetBadge(null);

		partial void SetBadge(string? value);
	}
}
#endif
