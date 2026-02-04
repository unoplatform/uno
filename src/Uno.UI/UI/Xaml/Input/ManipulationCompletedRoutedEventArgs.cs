using Windows.Foundation;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class ManipulationCompletedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		public ManipulationCompletedRoutedEventArgs() { }

		internal ManipulationCompletedRoutedEventArgs(UIElement source, UIElement container, ManipulationCompletedEventArgs args)
			: base(source)
		{
			Container = container;

			Pointers = args.Pointers;
			PointerDeviceType = args.PointerDeviceType;
			Position = FeatureConfiguration.ManipulationRoutedEventArgs.IsAbsolutePositionEnabled
				? args.Position
				: UIElement.GetTransform(container, null).Inverse().Transform(args.Position);
			Cumulative = args.Cumulative;
			Velocities = args.Velocities;
			IsInertial = args.IsInertial;
		}

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks>This collection might contains pointers that has been released.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal global::Windows.Devices.Input.PointerIdentifier[] Pointers { get; set; }

		public bool Handled { get; set; }

		public UIElement Container { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public bool IsInertial { get; }
	}
}
