// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using System.Linq;
using System.Reflection;
using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial class ManipulationUpdatedEventArgs
	{
		internal ManipulationUpdatedEventArgs(
			GestureRecognizer.Manipulation manipulation,
			PointerIdentifier[] pointers,
			Point position,
			ManipulationDelta delta,
			ManipulationDelta cumulative,
			ManipulationVelocities velocities,
			bool isInertial,
			uint contactCount,
			uint currentContactCount)
		{
			global::System.Diagnostics.Debug.Assert(pointers.Length > 0 && pointers.All(p => p.Type == pointers[0].Type));

			Manipulation = manipulation;
			Pointers = pointers;
			PointerDeviceType = (PointerDeviceType)pointers[0].Type;
			Position = position;
			Delta = delta;
			Cumulative = cumulative;
			Velocities = velocities;
			IsInertial = isInertial;
			ContactCount = contactCount;
			CurrentContactCount = currentContactCount;
		}

		internal GestureRecognizer.Manipulation Manipulation { get; }

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
		/// </summary>
		/// <remarks> This collection might contains pointers that has been released. <see cref="CurrentContactCount"/> gives the actual number of active pointers.</remarks>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal PointerIdentifier[] Pointers { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Delta { get; }
		public ManipulationDelta Cumulative { get; }
		public ManipulationVelocities Velocities { get; }
		public uint ContactCount { get; }
		public uint CurrentContactCount { get; }

		internal bool IsInertial { get; }
	}
}
#endif
