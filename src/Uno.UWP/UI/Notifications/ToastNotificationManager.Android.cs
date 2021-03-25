#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Notifications
{
	public partial class ToastNotificationManager
	{
		public static ToastNotifier CreateToastNotifier()
		{
			return new ToastNotifier();
		}
	}
}

#endif 
