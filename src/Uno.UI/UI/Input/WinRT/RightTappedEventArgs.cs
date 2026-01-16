using Windows.Devices.Input;
using Windows.Foundation;

namespace Microsoft.UI.Input;

public partial class RightTappedEventArgs
{
	internal RightTappedEventArgs(uint pointerId, PointerDeviceType type, Point position)
	{
		PointerId = pointerId;
		PointerDeviceType = type;
		Position = position;
	}

	public PointerDeviceType PointerDeviceType { get; }

	public Point Position { get; }

	internal uint PointerId { get; }

	[global::Uno.NotImplemented]
	public uint ContactCount
	{
		get
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RightTappedEventArgs", "uint RightTappedEventArgs.ContactCount");
			return 0;
		}
	}
}
