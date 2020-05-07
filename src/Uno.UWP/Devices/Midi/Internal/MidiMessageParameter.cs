using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Devices.Midi.Internal
{
    internal enum MidiMessageParameter
    {
        Channel = 15,
		Velocity = 127,
		Pressure = 127,
		Note = 127,
		Controller = 127,
		Control = 127,
		Bend = 16383
    }
}
