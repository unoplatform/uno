using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationCompletedRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		public ManipulationCompletedRoutedEventArgs() { }

		internal ManipulationCompletedRoutedEventArgs(
			object originalSource,
			UIElement container,
			PointerDeviceType pointerDeviceType,
			Point position,
			ManipulationDelta cumulative,
			bool isInertial)
			: base(originalSource)
		{
			Container = container;
			PointerDeviceType = pointerDeviceType;
			Position = position;
			Cumulative = cumulative;
			IsInertial = isInertial;
		}

		public bool Handled { get; set; }

		public UIElement Container { get; }

		public PointerDeviceType PointerDeviceType { get; }

		public Point Position { get; }

		public ManipulationDelta Cumulative { get; }

		public bool IsInertial { get; }
	}
}
