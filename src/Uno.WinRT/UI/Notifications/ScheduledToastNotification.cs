#nullable enable

using System;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{

	public partial class ScheduledToastNotification
	{
		private string _id = string.Empty;
		private string _tag = string.Empty;
		private string _group = string.Empty;

		public ScheduledToastNotification(XmlDocument content, DateTimeOffset deliveryTime)
		{
			Content = content ?? throw new ArgumentException("ScheduledToastNotification content cannot be null.", nameof(content));
			DeliveryTime = deliveryTime;
			ScheduleIdentifier = Guid.NewGuid().ToString("N");
		}

		public ScheduledToastNotification(XmlDocument content, DateTimeOffset deliveryTime, TimeSpan snoozeInterval, uint maximumSnoozeCount)
			: this(content, deliveryTime)
		{
			if (snoozeInterval < TimeSpan.FromMinutes(1) || snoozeInterval > TimeSpan.FromMinutes(60))
			{
				throw new ArgumentException("The snooze interval must be between 1 and 60 minutes.", nameof(snoozeInterval));
			}
			if (maximumSnoozeCount is < 1 or > 5)
			{
				throw new ArgumentException("The maximum snooze count must be between 1 and 5.", nameof(maximumSnoozeCount));
			}
			SnoozeInterval = snoozeInterval;
			MaximumSnoozeCount = maximumSnoozeCount;
		}

		public XmlDocument Content { get; }

		public DateTimeOffset DeliveryTime { get; }

		public DateTimeOffset? ExpirationTime { get; set; }

		public string Group
		{
			get => _group;
			set
			{
				ArgumentNullException.ThrowIfNull(value);
				ValidateIdentifierLength(value, 64, nameof(value));
				_group = value;
			}
		}

		public string Id
		{
			get => _id;
			set
			{
				ArgumentNullException.ThrowIfNull(value);
				ValidateIdentifierLength(value, 16, nameof(value));
				_id = value;
			}
		}

		public uint MaximumSnoozeCount { get; }

#if __ANDROID__
		public NotificationMirroring NotificationMirroring
		{
			get => SchedulingNotificationMirroring;
			set => SchedulingNotificationMirroring = value;
		}
#endif

		public TimeSpan? SnoozeInterval { get; }

		public bool SuppressPopup { get; set; }

		public string Tag
		{
			get => _tag;
			set
			{
				ArgumentNullException.ThrowIfNull(value);
				ValidateIdentifierLength(value, 64, nameof(value));
				_tag = value;
			}
		}

		internal string ScheduleIdentifier { get; set; }

		internal NotificationMirroring SchedulingNotificationMirroring { get; set; }

		private static void ValidateIdentifierLength(string value, int maximumLength, string parameterName)
		{
			if (value.Length > maximumLength)
			{
				throw new ArgumentException($"The identifier cannot exceed {maximumLength} characters.", parameterName);
			}
		}
	}
}
