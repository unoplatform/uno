using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
    public static class DataTemplateHelper
    {
		public static DataTemplate ResolveTemplate(DataTemplate dataTemplate, DataTemplateSelector dataTemplateSelector, object data)
		{
			return ResolveTemplate(dataTemplate, dataTemplateSelector, () => data);
		}

		public static DataTemplate ResolveTemplate(DataTemplate dataTemplate, DataTemplateSelector dataTemplateSelector, Func<object> data)
		{
			return dataTemplate
				?? dataTemplateSelector?.SelectTemplate(data?.Invoke());
		}
	}
}
