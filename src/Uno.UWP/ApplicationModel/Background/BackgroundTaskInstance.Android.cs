
#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{
	public partial class BackgroundTaskInstance : IBackgroundTaskInstance
	{
		internal BackgroundTaskRegistration Task { get; set; }

		BackgroundTaskRegistration IBackgroundTaskInstance.Task => Task;


		// below are required, because VStudio complains that IBackgroundTaskInstance defines these methods,
		// and it wants to implement interface.

		Guid IBackgroundTaskInstance.InstanceId => throw new NotImplementedException();

		uint IBackgroundTaskInstance.Progress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		uint IBackgroundTaskInstance.SuspendedCount => throw new NotImplementedException();

		object IBackgroundTaskInstance.TriggerDetails => throw new NotImplementedException();

		event BackgroundTaskCanceledEventHandler IBackgroundTaskInstance.Canceled
		{
			add
			{
				throw new NotImplementedException();
			}

			remove
			{
				throw new NotImplementedException();
			}
		}

		BackgroundTaskDeferral IBackgroundTaskInstance.GetDeferral()
		{
			throw new NotImplementedException();
		}
	}

}

#endif
