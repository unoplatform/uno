using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationDeltaRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		public ManipulationDeltaRoutedEventArgs() { }

		internal ManipulationDeltaRoutedEventArgs(
			object originalSource,
			UIElement container,
			PointerDeviceType pointerDeviceType,
			Point position,
			ManipulationDelta delta,
			ManipulationDelta cumulative,
			bool isInertial)
			: base(originalSource)
		{
			Container = container;
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Delta = delta;
			Cumulative = cumulative;
			IsInertial = isInertial;
		}

		public bool Handled { get; set; }

		public UIElement Container { get; }
		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public bool IsInertial { get; }

		public void Complete()
		{
		}
	}
}
