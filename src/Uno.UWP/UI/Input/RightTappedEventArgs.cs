using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
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
