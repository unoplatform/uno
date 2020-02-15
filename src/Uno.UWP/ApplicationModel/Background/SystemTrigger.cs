using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{
	public partial class SystemTrigger : IBackgroundTrigger
	{
		public  bool OneShot { get; }
		public SystemTriggerType TriggerType { get; }
#if !__ANDROID__
		// Android implementation checks if triggerType is supported
		public SystemTrigger( SystemTriggerType triggerType,  bool oneShot) 
		{
			OneShot = oneShot;
			TriggerType = triggerType;
		}
#endif
	}

}
