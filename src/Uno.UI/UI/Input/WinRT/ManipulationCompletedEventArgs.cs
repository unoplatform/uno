using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

public partial class ManipulationCompletedEventArgs
{
	internal ManipulationCompletedEventArgs(
		PointerIdentifier[] pointers,
		Point position,
		ManipulationDelta cumulative,
		ManipulationVelocities velocities,
		bool isInertial,
		uint contactCount,
		uint currentContactCount)
	{
		global::System.Diagnostics.Debug.Assert(pointers.Length > 0 && pointers.All(p => p.Type == pointers[0].Type));

		Pointers = pointers;
		Position = position;
		Cumulative = cumulative;
		Velocities = velocities;
		IsInertial = isInertial;
		ContactCount = contactCount;
		CurrentContactCount = currentContactCount;
	}

	/// <summary>
	/// Gets identifiers of all pointer that has been involved in that manipulation (cf. Remarks).
	/// </summary>
	/// <remarks>This collection might contains pointers that has been released. <see cref="CurrentContactCount"/> gives the actual number of active pointers.</remarks>
	/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
	internal PointerIdentifier[] Pointers { get; }

	public PointerDeviceType PointerDeviceType => (PointerDeviceType)Pointers[0].Type;
	public Point Position { get; }
	public ManipulationDelta Cumulative { get; }
	public ManipulationVelocities Velocities { get; }
	public uint ContactCount { get; }
	public uint CurrentContactCount { get; }
	internal bool IsInertial { get; }
}
