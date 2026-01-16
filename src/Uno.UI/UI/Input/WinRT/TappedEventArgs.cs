using Windows.Devices.Input;
using Windows.Foundation;

namespace Microsoft.UI.Input;

public partial class TappedEventArgs
{
	internal TappedEventArgs(uint pointerId, PointerDeviceType type, Point position, uint tapCount)
	{
		PointerId = pointerId;
		PointerDeviceType = type;
		Position = position;
		TapCount = tapCount;
	}

	public PointerDeviceType PointerDeviceType { get; }

	public Point Position { get; }

	internal uint PointerId { get; }

	public uint TapCount { get; }

	[global::Uno.NotImplemented]
	public uint ContactCount
	{
		get
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.TappedEventArgs", "uint TappedEventArgs.ContactCount");
			return 0;
		}
	}
}
