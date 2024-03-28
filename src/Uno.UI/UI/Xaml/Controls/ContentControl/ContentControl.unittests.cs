using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentControl : Control
	{
		private new bool HasParent() => true;

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
