#if __IOS__ || __MACOS__ || __WASM__ || __SKIA__
using System;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class BadgeNotification
	{
		public BadgeNotification(XmlDocument content)
		{
			Content = content ?? throw new ArgumentNullException(nameof(content));
		}

		public XmlDocument Content { get; }
	}
}
#endif
