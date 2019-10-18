using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationDeltaRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly GestureRecognizer _recognizer;

		public ManipulationDeltaRoutedEventArgs() { }

		internal ManipulationDeltaRoutedEventArgs(UIElement container, GestureRecognizer recognizer, ManipulationUpdatedEventArgs args)
			: base(container)
		{
			Container = container;

			_recognizer = recognizer;

			PointerDeviceType = args.PointerDeviceType;
			Position = args.Position;
			Delta = args.Delta;
			Cumulative = args.Cumulative;
			IsInertial = args.IsInertial;
		}

		public bool Handled { get; set; }

		public UIElement Container { get; }
		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public bool IsInertial { get; }

		public void Complete()
			=> _recognizer?.CompleteGesture();
	}
}
