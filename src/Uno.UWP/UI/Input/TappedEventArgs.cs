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
		internal TappedEventArgs(PointerDeviceType type, Point position, uint tapCount)
		{
			PointerDeviceType = type;
			Position = position;
			TapCount = tapCount;
		}

		public PointerDeviceType PointerDeviceType { get; }

		public Point Position { get; }

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
