using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Uno.Foundation.Extensibility.ApiExtension(typeof(Windows.UI.ViewManagement.IApplicationViewSpanningRects), typeof(Uno.UI.DualScreen.DuoApplicationViewSpanningRects))]
[assembly: Uno.Foundation.Extensibility.ApiExtension(typeof(Uno.Devices.Sensors.INativeHingeAngleSensor), typeof(Uno.UI.DualScreen.DuoHingeAngleSensor))]
