using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Content))]
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
