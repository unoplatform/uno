using Windows.Foundation;
using Uno.UI.Xaml.Input;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationDeltaRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly GestureRecognizer _recognizer;

		public ManipulationDeltaRoutedEventArgs() { }

		internal ManipulationDeltaRoutedEventArgs(UIElement source, UIElement container, GestureRecognizer recognizer, ManipulationUpdatedEventArgs args)
			: base(source)
		{
			Container = container;

			_recognizer = recognizer;

			Pointers = args.Pointers;
			PointerDeviceType = args.PointerDeviceType;
			Position = args.Position;
			Delta = args.Delta;
			Cumulative = args.Cumulative;
			Velocities = args.Velocities;
			IsInertial = args.IsInertial;
		}

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks>This collection might contains pointers that has been released.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal global::Windows.Devices.Input.PointerIdentifier[] Pointers { get; }

		public bool Handled { get; set; }

		public UIElement Container { get; }
		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public bool IsInertial { get; }

		public void Complete()
			=> _recognizer?.CompleteGesture();
	}
}
