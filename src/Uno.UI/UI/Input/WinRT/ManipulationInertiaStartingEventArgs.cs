// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Uno;

namespace Microsoft.UI.Input
{
	public partial class ManipulationInertiaStartingEventArgs
	{
		internal ManipulationInertiaStartingEventArgs(
			GestureRecognizer.Manipulation manipulation,
			PointerIdentifier[] pointers,
			Point position,
			ManipulationDelta delta,
			ManipulationDelta cumulative,
			ManipulationVelocities velocities,
			uint contactCount)
		{
			global::System.Diagnostics.Debug.Assert(pointers.Length > 0 && pointers.All(p => p.Type == pointers[0].Type));

			Pointers = pointers;
			PointerDeviceType = (PointerDeviceType)pointers[0].Type;
			Position = position;
			Delta = delta;
			Cumulative = cumulative;
			Velocities = velocities;
			ContactCount = contactCount;

			Manipulation = manipulation;
		}

		internal GestureRecognizer.Manipulation Manipulation { get; }

		/// <summary>
		/// **Uno specific**
		/// Determines if the inertia should be run in sync with the CompositionTarget.Rending event or not.
		/// This impacts only platform that have a correct implementation of the CompositionTarget.Rending event, i.e. those using skia backend.
		/// Default is `true`.
		/// </summary>
		[UnoOnly]
		public bool UseCompositionTimer { get; set; } = true;

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks>This collection might contains pointers that has been released. <see cref="ContactCount"/> gives the actual number of active pointers.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal PointerIdentifier[] Pointers { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public uint ContactCount { get; }
	}
}
#endif
