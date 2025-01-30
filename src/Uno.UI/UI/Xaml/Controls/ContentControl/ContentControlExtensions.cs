using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public static class ContentControlExtensions
	{
		public static DataTemplate ResolveContentTemplate(this ContentControl contentControl)
		{
			return DataTemplateHelper.ResolveTemplate(
				contentControl?.ContentTemplate,
				contentControl?.ContentTemplateSelector,
				contentControl?.Content,
				contentControl
			);
		}
	}
}
