using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Notifications
{
	public partial class ToastNotification
	{
		public DateTimeOffset? ExpirationTime { get; set; }

		public NotificationMirroring NotificationMirroring { get; set; }

	}
}
