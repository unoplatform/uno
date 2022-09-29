using AppKit;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeUpdater
	{
		partial void SetBadge(string? value)
		{
			NSApplication.SharedApplication.DockTile.BadgeLabel = value ?? "";
		}
	}
}
