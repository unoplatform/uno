#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.UI.Xaml.Controls.Layouter
{
	internal interface ISetLayoutSlots
	{
		// On iOS lots of elements does not use the Layouter to measure themselves and their children,
		// doing this the are bypassing some important steps, including setting LayoutInformation.LayoutSlot.

		// This marker interface is designed to flag elements that does not use the Layouter,
		// but which are still taking care to update that LayoutInformation.LayoutSlot for its children.
		// (The LayoutSlot is set by parent on its children !).

		// This is to avoid massive refactoring of existing elements and MUST NOT be used in new development,
		// use the Layouter instead!

		// Note: For future dev, we might need to add an equivalent marker interface like IUseLayouter!
	}
}
