using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentControl : Control
	{
		partial void RegisterContentTemplateRoot()
		{
			AddChild((FrameworkElement)ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			RemoveChild((FrameworkElement)ContentTemplateRoot);
		}
	}
}
