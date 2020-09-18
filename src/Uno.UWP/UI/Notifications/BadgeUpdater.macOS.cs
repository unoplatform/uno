#if __MACOS__
using AppKit;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater 
	{
		public  void Update(BadgeNotification notification)
		{
			var element = notification.Content.SelectSingleNode("/badge") as XmlElement;
			var attributeValue = element?.GetAttribute("value");
			NSApplication.SharedApplication.DockTile.BadgeLabel = attributeValue ?? "";
		}

		public  void Clear()
		{
			NSApplication.SharedApplication.DockTile.BadgeLabel = "";
		}
	}
}
#endif