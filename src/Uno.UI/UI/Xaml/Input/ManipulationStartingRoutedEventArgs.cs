using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationStartingRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		public ManipulationStartingRoutedEventArgs() { }

		internal ManipulationStartingRoutedEventArgs(
			object originalSource,
			UIElement container,
			ManipulationModes mode)
			: base(originalSource)
		{
			Container = container;
			Mode = mode;
		}

		public bool Handled { get; set; }

		public UIElement Container { get; set; }
		public ManipulationModes Mode { get; set; }
	}
}
