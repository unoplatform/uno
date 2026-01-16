using System;
using Uno.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Input
{
	public partial class ManipulationInertiaStartingRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly GestureRecognizer.Manipulation.InertiaProcessor _processor;

		public ManipulationInertiaStartingRoutedEventArgs() { }

		internal ManipulationInertiaStartingRoutedEventArgs(UIElement source, UIElement container, ManipulationInertiaStartingEventArgs args)
			: base(source)
		{
			Container = container;
			Manipulation = args.Manipulation;
			_processor = args.Manipulation.Inertia ?? throw new InvalidOperationException("Inertia processor is not available.");

			Pointers = args.Pointers;
			PointerDeviceType = args.PointerDeviceType;
			Delta = args.Delta;
			Cumulative = args.Cumulative;
			Velocities = args.Velocities;

			TranslationBehavior = new InertiaTranslationBehavior(_processor);
			RotationBehavior = new InertiaRotationBehavior(_processor);
			ExpansionBehavior = new InertiaExpansionBehavior(_processor);
		}

		internal GestureRecognizer.Manipulation Manipulation { get; }

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks>This collection might contains pointers that has been released.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal global::Windows.Devices.Input.PointerIdentifier[] Pointers { get; }

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
