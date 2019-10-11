using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationStartingRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		public ManipulationStartingRoutedEventArgs() { }

		internal ManipulationStartingRoutedEventArgs(UIElement container)
			: base(container)
		{
			Container = container;
			Mode = container.ManipulationMode;
		}

		public bool Handled { get; set; }

		public UIElement Container { get; set; }
		public ManipulationModes Mode { get; set; }
	}
}
