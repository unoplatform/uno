using System;
using System.Linq;

namespace Windows.UI.Input
{
	internal partial class ManipulationStartingEventArgs
	{
		// Be aware that this class is not part of the UWP contract

		internal ManipulationStartingEventArgs(GestureSettings settings)
		{
			Settings = settings;
		}

		public GestureSettings Settings { get; set; }
	}
}
