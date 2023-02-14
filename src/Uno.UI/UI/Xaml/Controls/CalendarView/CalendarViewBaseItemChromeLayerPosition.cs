using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	// the sequence that CCalendarViewBaseItemChrome needs to render
	// (in order of lowest layer to highest layer).
	internal enum CalendarViewBaseItemChromeLayerPosition
	{
		Pre,
		TemplateChild_Post, // after TemplateChild, before Chrome TextBlocks - for the density colors layer
		Post,
	};
}
