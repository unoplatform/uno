// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial class ManipulationStartedEventArgs
	{
		internal ManipulationStartedEventArgs(
			PointerIdentifier[] pointers,
			Point position,
			ManipulationDelta cumulative,
			uint contactCount)
		{
			global::System.Diagnostics.Debug.Assert(contactCount == pointers.Length, "We should have the same number of pointers for the manip start.");
			global::System.Diagnostics.Debug.Assert(pointers.Length > 0 && pointers.All(p => p.Type == pointers[0].Type));

			Pointers = pointers;
			PointerDeviceType = (PointerDeviceType)pointers[0].Type;
			Position = position;
			Cumulative = cumulative;
			ContactCount = contactCount;
		}

		/// <summary>
		/// Gets identifiers of all pointer that has been involved in that manipulation.
		/// </summary>
		/// <remarks>All pointers are expected to have the same <see cref="PointerIdentifier.Type"/>.</remarks>
		internal PointerIdentifier[] Pointers { get; }

		public PointerDeviceType PointerDeviceType { get; }
		public Point Position { get; }
		public ManipulationDelta Cumulative { get; }
		public uint ContactCount { get; }
	}
}
#endif
