using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public static class ContentPresenterExtensions
	{
		public static DataTemplate ResolveContentTemplate(this ContentPresenter contentPresenter)
		{
			return DataTemplateHelper.ResolveTemplate(
				contentPresenter?.ContentTemplate,
				contentPresenter?.ContentTemplateSelector,
				contentPresenter?.Content,
				contentPresenter
			);
		}
	}
}
