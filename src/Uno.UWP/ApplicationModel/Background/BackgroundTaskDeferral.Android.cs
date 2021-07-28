using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.Background
{
	public partial class BackgroundTaskDeferral
	{
		/// <summary>
		/// On Android, BackgroundTaskDeferral has no meaning (background tasks will not be cancelled by OS, unless it takes really much time)
		/// </summary>
		public void Complete()
		{
			// on Android, there is nothing to do
		}
	}
}

