using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public  partial class DraggingEventArgs 
	{
		internal DraggingEventArgs(DraggingState state, PointerDeviceType type, Point position)
		{
			DraggingState = state;
			PointerDeviceType = type;
			Position = position;
		}

		public DraggingState DraggingState { get; }

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
