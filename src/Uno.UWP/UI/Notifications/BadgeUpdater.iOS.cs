#if __IOS__
using UIKit;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater 
	{
		public  void Update(BadgeNotification notification)
		{
			var element = notification.Content.SelectSingleNode("/badge") as XmlElement;
			var attributeValue = element?.GetAttribute("value");
			if (int.TryParse(attributeValue, out var badgeNumber))
			{
				UIApplication.SharedApplication.ApplicationIconBadgeNumber = badgeNumber;
			}
			else
			{
				Clear();
			}
		}

		public  void Clear()
		{
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
		}
	}
}
#endif