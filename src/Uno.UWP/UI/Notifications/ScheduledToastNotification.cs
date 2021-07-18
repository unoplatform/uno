using Uno.Logging;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{

	public partial class ScheduledToastNotification
	{
		public  XmlDocument Content { get; internal set; }
		public global::System.DateTimeOffset DeliveryTime { get; internal set; }

		private string _tag;

		public string Tag
		{
			get
			{
				return _tag;
			}
			set
			{
				_tag = value;
				if (_tag.Length > 64)
				{
					// UWP limit: 16 chars, since Creators Update (15063) - 64 characters
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning("Windows.UI.Notifications.ScheduledToastNotification.Tag is set to string longer than UWP limit");
					}
				}
			}
		}

#if __ANDROID__
		public  NotificationMirroring NotificationMirroring { get; set; }

		public global::System.DateTimeOffset? ExpirationTime { get; set; }
#endif

		public ScheduledToastNotification( XmlDocument content, global::System.DateTimeOffset deliveryTime) 
		{
			Content = content;
			DeliveryTime = deliveryTime;
		}

	}
}
