using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Diagnostics.Eventing
{
	public enum EventOpcode : byte
	{
		Info,
		Start,
		Stop,
		DataCollectionStart,
		DataCollectionStop,
		Extension,
		Reply,
		Resume,
		Suspend,
		Send,
		Receive = 240
	}
}
