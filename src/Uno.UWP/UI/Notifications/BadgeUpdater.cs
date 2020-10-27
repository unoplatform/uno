#if __IOS__ || __MACOS__ || __SKIA__ || __WASM__ || __NETSTD_REFERENCE__

#nullable enable

using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		internal BadgeUpdater()
		{
			InitPlatform();
		}

		partial void InitPlatform();

		public void Update(BadgeNotification notification)
		{
			var element = notification.Content.SelectSingleNode("/badge") as XmlElement;
			var attributeValue = element?.GetAttribute("value");
			SetBadge(attributeValue);
		}

		public void Clear() => SetBadge(null);

		partial void SetBadge(string? value);
	}
}
#endif
