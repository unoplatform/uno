
#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Background
{
	public partial class BackgroundExecutionManager
	{
		private static async Task<BackgroundAccessStatus> RequestAccessAsyncTask()
			=> BackgroundAccessStatus.AlwaysAllowed;

		public static IAsyncOperation<BackgroundAccessStatus> RequestAccessAsync()
			=> RequestAccessAsyncTask().AsAsyncOperation<BackgroundAccessStatus>();

	}

}

#endif 
