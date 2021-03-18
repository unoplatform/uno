using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Devices.Sensors.Helpers;
using Windows.Devices.Sensors;
using Windows.Foundation;

namespace Uno.Devices.Sensors
{
    public interface INativeHingeAngleSensor
    {
		bool DeviceHasHinge { get; }

		event EventHandler<NativeHingeAngleReading> ReadingChanged;
    }
}
