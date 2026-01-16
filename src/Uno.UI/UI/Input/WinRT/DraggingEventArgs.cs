using Windows.Devices.Input;
using Windows.Foundation;

namespace Microsoft.UI.Input;

public partial class DraggingEventArgs
{
	internal DraggingEventArgs(PointerPoint point, DraggingState state, uint contactCount)
	{
		Pointer = point;
		DraggingState = state;
		ContactCount = contactCount;
	}

	internal PointerPoint Pointer { get; }

	public DraggingState DraggingState { get; }

	public PointerDeviceType PointerDeviceType => (PointerDeviceType)Pointer.PointerDevice.PointerDeviceType;

	public Point Position => Pointer.Position;

	public uint ContactCount { get; }
}
