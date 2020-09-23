using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Input
{
	public  partial class DraggingEventArgs 
	{
		internal DraggingEventArgs(PointerPoint point, DraggingState state)
		{
			Pointer = point;
			DraggingState = state;
		}

		internal PointerPoint Pointer { get; }

		public DraggingState DraggingState { get; }

		public PointerDeviceType PointerDeviceType => Pointer.PointerDevice.PointerDeviceType;

		public Point Position => Pointer.Position;

		[global::Uno.NotImplemented]
		public uint ContactCount
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.DraggingEventArgs", "uint DraggingEventArgs.ContactCount");
				return 0;
			}
		}
	}
}
