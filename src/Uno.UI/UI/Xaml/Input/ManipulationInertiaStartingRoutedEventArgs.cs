using Windows.Devices.Input;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public  partial class ManipulationInertiaStartingRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		public ManipulationInertiaStartingRoutedEventArgs() { }

		internal ManipulationInertiaStartingRoutedEventArgs(UIElement container, ManipulationInertiaStartingEventArgs args)
			: base(container)
		{
			Container = container;

			PointerDeviceType = args.PointerDeviceType;
			Delta = args.Delta;
			Cumulative = args.Cumulative;
		}

		public bool Handled { get; set; }

		public UIElement Container { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
	}
}
