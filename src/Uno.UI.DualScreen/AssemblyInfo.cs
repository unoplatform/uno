using Windows.UI.ViewManagement;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;
using Uno.UI.DualScreen;

#if __ANDROID__
[assembly: ApiExtension(typeof(IApplicationViewSpanningRects), typeof(DuoApplicationViewSpanningRects))]
[assembly: ApiExtension(typeof(INativeHingeAngleSensor), typeof(DuoHingeAngleSensor))]
#endif
