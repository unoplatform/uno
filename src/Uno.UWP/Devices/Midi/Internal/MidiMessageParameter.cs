using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Devices.Midi.Internal
{
	internal enum MidiMessageParameter
	{
		Frame = 7,
		FrameValues = 15,
		Channel = 15,
		Velocity = 127,
		Pressure = 127,
		Note = 127,
		Controller = 127,
		ControlValue = 127,
		Program = 127,
		Song = 127,
		Bend = 16383,
		Beats = 16383,
	}
}
