using Uno.Logging;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications
{
	public partial class ToastNotification
	{
		private string _tag;

		public ToastNotification(XmlDocument content)
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
