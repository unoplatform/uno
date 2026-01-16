using System;
using System.Linq;
using Windows.Devices.Input;

namespace Microsoft.UI.Input;

internal partial class ManipulationStartingEventArgs
{
	// Be aware that this class is not part of the UWP contract

	internal ManipulationStartingEventArgs(PointerIdentifier pointer, GestureSettings settings)
	{
		Pointer = pointer;
		Settings = settings;
	}

	/// <summary>
	/// Gets identifier of the first pointer for which a manipulation is considered
	/// </summary>
	internal PointerIdentifier Pointer { get; }

	public GestureSettings Settings { get; set; }
}
