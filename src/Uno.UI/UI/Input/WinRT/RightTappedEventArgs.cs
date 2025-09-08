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
}
#endif
