
#nullable enable

using Uno;
using Windows.Foundation;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Toolkit.DevTools.Input;

internal interface IInjectedPointer
{
	void Press(Point position);

	void MoveTo(Point position, uint? steps = null, uint? stepOffsetInMilliseconds = null);

	void MoveBy(double deltaX = 0, double deltaY = 0);

	void Release();
}
