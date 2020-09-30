using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public  partial class RightTappedEventArgs 
	{
		internal RightTappedEventArgs(PointerDeviceType type, Point position)
		{
			PointerDeviceType = type;
			Position = position;
		}

		public PointerDeviceType PointerDeviceType { get; }

		public Point Position { get; }

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
