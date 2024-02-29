using Uno.Foundation.Logging;
using Uno.Extensions;

using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class ToastNotification
	{
		private string _tag;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. - TODO: Fix nullability annotation.
		public ToastNotification(XmlDocument content)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
			Content = content;
		}

		public XmlDocument Content { get; internal set; }

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
						this.Log().LogWarning("Windows.UI.Notifications.ToastNotification.Tag is set to string longer than UWP limit");
					}
				}
			}
		}
	}
}
