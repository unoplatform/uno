using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;
using Uno.UI.UI.Xaml.Input.Internal;

namespace Windows.UI.Xaml.Input
{
	public partial class ManipulationCompletedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		public ManipulationCompletedRoutedEventArgs() { }

		internal ManipulationCompletedRoutedEventArgs(UIElement container, ManipulationCompletedEventArgs args)
			: base(container)
		{
			Container = container;

			Pointers = args.Pointers;
			PointerDeviceType = args.PointerDeviceType;

			// ManipulationCompletedEventArgs.Position is absolute position. We want to
			// have a relative point to the element being manipulated to match UWP
			// NOTE: This is a workaround, and this scenario is still broken for some cases.
			// See https://github.com/unoplatform/uno/issues/6964#issuecomment-913041138 for details.
			Position = ManipulationEventArgsHelpers.MapPointRelativeTo(container, args.Position);

			Cumulative = args.Cumulative;
			Velocities = args.Velocities;
			IsInertial = args.IsInertial;
		}

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks>This collection might contains pointers that has been released.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal PointerIdentifier[] Pointers { get; set; }

		public bool Handled { get; set; }

		public UIElement Container { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public bool IsInertial { get; }
	}
}
