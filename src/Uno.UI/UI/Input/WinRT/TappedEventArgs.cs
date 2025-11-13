// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
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
}
#endif
