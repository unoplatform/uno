using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class UserControl : Control
	{
		public UserControl()
		{
		}

		// This mimics UWP
		private protected override Type GetDefaultStyleKey() => null;

		private void OnContentChanged(UIElement oldContent, UIElement newContent)
		{
			if (oldContent is not null)
			{
				RemoveChild(oldContent);
			}

			if (newContent is not null)
			{
				AddChild(newContent);
			}
		}
	}
}
