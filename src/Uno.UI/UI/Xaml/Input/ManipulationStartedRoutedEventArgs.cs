using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationStartedRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly GestureRecognizer _recognizer;

		public ManipulationStartedRoutedEventArgs() { }

		internal ManipulationStartedRoutedEventArgs(UIElement container, GestureRecognizer recognizer, ManipulationStartedEventArgs args)
			: base(container)
		{
			Container = container;

			_recognizer = recognizer;

			PointerDeviceType = args.PointerDeviceType;
			Position = args.Position;
			Cumulative = args.Cumulative;
		}


		public bool Handled { get; set; }

		public UIElement Container { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }

		public void Complete()
			=> _recognizer?.CompleteGesture();
	}
}
