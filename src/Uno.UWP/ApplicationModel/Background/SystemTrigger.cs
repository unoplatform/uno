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
		public SystemTrigger( SystemTriggerType triggerType,  bool oneShot) 
		{
			OneShot = oneShot;
			TriggerType = triggerType;
		}
	}

}
