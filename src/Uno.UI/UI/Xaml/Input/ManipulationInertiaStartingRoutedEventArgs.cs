using Windows.Devices.Input;
using Windows.UI.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public  partial class ManipulationInertiaStartingRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		public ManipulationInertiaStartingRoutedEventArgs() { }

		internal ManipulationInertiaStartingRoutedEventArgs(UIElement container, ManipulationInertiaStartingEventArgs args)
			: base(container)
		{
			Container = container;

			Pointers = args.Pointers;
			PointerDeviceType = args.PointerDeviceType;
			Delta = args.Delta;
			Cumulative = args.Cumulative;
			Velocities = args.Velocities;

			TranslationBehavior = new InertiaTranslationBehavior(args.Processor);
			RotationBehavior = new InertiaRotationBehavior(args.Processor);
			ExpansionBehavior = new InertiaExpansionBehavior(args.Processor);
		}

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks>This collection might contains pointers that has been released.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal PointerIdentifier[] Pointers { get; }

		public bool Handled { get; set; }

		public UIElement Container { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }

		public InertiaTranslationBehavior TranslationBehavior { get; set; } // Ctor is internal, so we don't support external set!
		public InertiaRotationBehavior RotationBehavior { get; set; } // Ctor is internal, so we don't support external set!
		public InertiaExpansionBehavior ExpansionBehavior { get; set; } // Ctor is internal, so we don't support external set!
	}
}
