#if __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Midi
{
	public partial interface IMidiMessage
	{
		MidiMessageType Type
		{
			get;
		}
	}
}
#endif
