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
#pragma warning disable CA1822 // Mark members as static
		public uint ContactCount
#pragma warning restore CA1822 // Mark members as static
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RightTappedEventArgs", "uint RightTappedEventArgs.ContactCount");
				return 0;
			}
		}
	}
}
