
#if __ANDROID__

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
			switch(triggerType)
			{ // supported types
				case SystemTriggerType.ServicingComplete:
				case SystemTriggerType.SmsReceived:
				case SystemTriggerType.TimeZoneChange:
				case SystemTriggerType.UserAway:
				case SystemTriggerType.UserPresent:
					break;
				default:	// all other types: unsupported
					throw new NotSupportedException("Unimplemented type of SystemTrigger");
			}
		}
	}

}

#endif
