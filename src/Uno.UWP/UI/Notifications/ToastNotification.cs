using System;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class ToastNotification
	{
		private string _tag = string.Empty;
		private string _group = string.Empty;

		public ToastNotification(XmlDocument content)
		{
			Content = content ?? throw new ArgumentException("ToastNotification content cannot be null.", nameof(content));
		}

		public XmlDocument Content { get; internal set; }

		public DateTimeOffset? ExpirationTime { get; set; }

		public bool ExpiresOnReboot { get; set; }

		public string Group
		{
			get => _group;
			set
			{
				ArgumentNullException.ThrowIfNull(value);
				ValidateIdentifierLength(value, nameof(value));
				_group = value;
			}
		}

		public ToastNotificationPriority Priority { get; set; }

		public bool SuppressPopup { get; set; }

		public string Tag
		{
			get
			{
				return _tag;
			}
			set
			{
				ArgumentNullException.ThrowIfNull(value);
				if (value.Length == 0)
				{
					throw new ArgumentException("A non-empty notification tag is required.", nameof(value));
				}
				ValidateIdentifierLength(value, nameof(value));
				_tag = value;
			}
		}

		internal uint AppNotificationId { get; set; }

		private static void ValidateIdentifierLength(string value, string parameterName)
		{
			if (value.Length > 64)
			{
				throw new ArgumentException("Notification identifiers cannot exceed 64 characters.", parameterName);
			}
		}
	}
}
