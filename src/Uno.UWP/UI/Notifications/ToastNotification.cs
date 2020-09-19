
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.UI.Notifications
{
	public partial class ToastNotification
	{

		private string _Tag;
		public string Tag {
			get
			{
				return this._Tag;
			}
			set
			{
				this._Tag = value;
				if(this._Tag.Length > 64)
				{
					// UWP limit: 16 chars, since Creators Update (15063) - 64 characters
					this.Log().Warn("Windows.UI.Notifications.ToastNotification.Tag is set to string longer than UWP limit");
				}
			}
		} 

		public Windows.Data.Xml.Dom.XmlDocument Content { get; internal set; }

		public ToastNotification(Windows.Data.Xml.Dom.XmlDocument content)
		{
			Content = content;
		}

	}
}
