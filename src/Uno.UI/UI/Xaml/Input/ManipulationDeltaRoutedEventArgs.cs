using System;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Xaml.Input;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Input
{
	public partial class ManipulationDeltaRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly GestureRecognizer _recognizer;

		public ManipulationDeltaRoutedEventArgs() { }

		internal ManipulationDeltaRoutedEventArgs(UIElement source, UIElement container, GestureRecognizer recognizer, ManipulationUpdatedEventArgs args)
			: base(source)
		{
			Container = container;
			Manipulation = args.Manipulation;

			_recognizer = recognizer;

			Pointers = args.Pointers;
			PointerDeviceType = args.PointerDeviceType;
			Position = FeatureConfiguration.ManipulationRoutedEventArgs.IsAbsolutePositionEnabled
				? args.Position
				: UIElement.GetTransform(container, null).Inverse().Transform(args.Position);
			Delta = args.Delta;
			Cumulative = args.Cumulative;
			Velocities = args.Velocities;
			IsInertial = args.IsInertial;
		}

		internal GestureRecognizer.Manipulation Manipulation { get; }

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks>This collection might contains pointers that has been released.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="Type"/>.</remarks>
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
