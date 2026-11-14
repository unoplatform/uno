using Windows.Devices.Input;
using Windows.Foundation;

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

public partial class TappedEventArgs
{
	internal TappedEventArgs(uint pointerId, PointerDeviceType type, Point position, Point rawPosition, uint tapCount)
	{
		PointerId = pointerId;
		PointerDeviceType = type;
		Position = position;
		RawPosition = rawPosition;
		TapCount = tapCount;
	}

	public PointerDeviceType PointerDeviceType { get; }

	public Point Position { get; }

	/// <summary>
	/// The <see cref="PointerPoint.RawPosition"/> of the press (the second one for a double-tap),
	/// which is relative to the window for a UIElement recognizer.
	/// </summary>
	internal Point RawPosition { get; }

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
