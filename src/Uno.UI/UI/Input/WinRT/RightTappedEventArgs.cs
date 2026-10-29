using Windows.Devices.Input;
using Windows.Foundation;

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

public partial class RightTappedEventArgs
{
	internal RightTappedEventArgs(uint pointerId, PointerDeviceType type, Point position, Point rawPosition)
	{
		PointerId = pointerId;
		PointerDeviceType = type;
		Position = position;
		RawPosition = rawPosition;
	}

	public PointerDeviceType PointerDeviceType { get; }

	public Point Position { get; }

	/// <summary>
	/// The <see cref="PointerPoint.RawPosition"/> of the press, which is relative to the window for a UIElement recognizer.
	/// </summary>
	internal Point RawPosition { get; }

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
