using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class UserControl : ContentControl
	{
		public UserControl()
		{
		}

		// This mimics UWP
		private protected override Type GetDefaultStyleKey() => null;

#if UNO_HAS_ENHANCED_LIFECYCLE
		protected override void OnContentChanged(object oldContent, object newContent)
		{
			// NOTE: In WinUI, this logic is in CUserControl::SetContent (which is in Control.cpp - Yes, not UserControl_Partial.cpp)
			// In Uno, we incorrectly inherit from ContentControl, so we override OnContentChange
			if (oldContent is UIElement oldUIElement)
			{
				RemoveChild(oldUIElement);
			}

			if (newContent is UIElement newUIElement)
			{
				AddChild(newUIElement);
			}
		}
#endif
	}
}
