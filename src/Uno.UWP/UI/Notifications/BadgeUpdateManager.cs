#if __IOS__ || __MACOS__
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	/// <summary>
	/// Creates BadgeUpdater objects that you use to manipulate a tile's badge overlay.
	/// This class also provides access to the XML content of the system-provided badge 
	/// templates so that you can customize that content for use in updating your badges.
	/// </summary>
	public static partial class BadgeUpdateManager 
	{
		/// <summary>
		/// Creates and initializes a new instance of the BadgeUpdater, which lets you
		/// change the appearance or content of the badge on the calling app's tile.
		/// </summary>
		/// <returns>Badge updater.</returns>
		public static BadgeUpdater CreateBadgeUpdaterForApplication() => new BadgeUpdater();

		/// <summary>
		/// Gets the XML content of one of the predefined badge templates
		/// so that you can customize it for a badge update.
		/// </summary>
		/// <param name="type">The type of badge template, either a glyph or a number.</param>
		/// <returns>The object that contains the template XML.</returns>
		public static XmlDocument GetTemplateContent(BadgeTemplateType type)
		{
			// Although UWP has two "template types", both return the same XML.
			var xml = new XmlDocument();
			xml.LoadXml("<badge value=\"\" />");
			return xml;
		}
	}
}
#endif