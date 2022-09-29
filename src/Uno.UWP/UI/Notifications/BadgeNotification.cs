#nullable disable

#if !__ANDROID__
using System;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	/// <summary>
	/// Defines the content, associated metadata, and expiration time of an update to a tile's badge overlay.
	/// A badge can display a number from 1 to 99 or a status glyph.
	/// </summary>
	public partial class BadgeNotification
	{
		/// <summary>
		/// Creates and initializes a new instance of the BadgeNotification.
		/// </summary>
		/// <param name="content">The XML content that defines the badge update.</param>
		public BadgeNotification(XmlDocument content)
		{
			Content = content ?? throw new ArgumentNullException(nameof(content));
		}

		/// <summary>
		/// Gets the XML that defines the value or glyph used as the tile's badge.
		/// </summary>
		public XmlDocument Content { get; }
	}
}
#endif
