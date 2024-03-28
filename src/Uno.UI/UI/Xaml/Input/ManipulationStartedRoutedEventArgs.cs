using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationStartedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly GestureRecognizer _recognizer;

		public ManipulationStartedRoutedEventArgs() { }

		internal ManipulationStartedRoutedEventArgs(UIElement source, UIElement container, GestureRecognizer recognizer, ManipulationStartedEventArgs args)
			: base(source)
		{
			Container = container;

			_recognizer = recognizer;

			Pointers = args.Pointers;
			PointerDeviceType = args.PointerDeviceType;
			Position = args.Position;
			Cumulative = args.Cumulative;
		}

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation.
		/// </summary>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal PointerIdentifier[] Pointers { get; }

		public bool Handled { get; set; }

		public UIElement Container { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }

		public void Complete()
			=> _recognizer?.CompleteGesture();
	}
}
