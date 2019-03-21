using System;
using Uno.UI.Controls;

namespace Windows.UI.ViewManagement
{
	public partial class InputPaneVisibilityEventArgs
	{
		internal InputPaneVisibilityEventArgs(Foundation.Rect occludedRect)
		{
			OccludedRect = occludedRect;
		}

		public bool EnsuredFocusedElementInView { get; set; }

		public Foundation.Rect OccludedRect { get; private set; }
	}
}
