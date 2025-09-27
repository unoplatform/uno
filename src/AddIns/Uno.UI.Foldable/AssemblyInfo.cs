using Windows.UI.ViewManagement;
using Uno.Devices.Sensors;
using Uno.Foundation.Extensibility;
using Uno.UI.Foldable;

#if __ANDROID__
[assembly: ApiExtension(typeof(IApplicationViewSpanningRects), typeof(FoldableApplicationViewSpanningRects))]
[assembly: ApiExtension(typeof(INativeHingeAngleSensor), typeof(FoldableHingeAngleSensor))]
#endif
