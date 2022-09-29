using Uno.Foundation.Logging;
using Uno.Extensions;

using Windows.Data.Xml.Dom;
using System;

namespace Windows.UI.Notifications
{

    public partial class ScheduledToastNotification
    {

        public ScheduledToastNotification(XmlDocument content, DateTimeOffset deliveryTime)
        {
            if (content is null)
            {
                // yes, UWP throws here ArgumentException, and not ArgumentNullException
                throw new ArgumentException("ScheduledToastNotification constructor: XmlDocument content cannot be null");
            }
            Content = content;
            DeliveryTime = deliveryTime;
        }

        public XmlDocument Content { get; internal set; }
        public DateTimeOffset DeliveryTime { get; internal set; }

        private string _tag = "";

        public string Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException("ScheduledToastNotification.Tag cannot be null");
                }
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


    }
}
