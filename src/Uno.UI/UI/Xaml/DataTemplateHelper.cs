using Uno.UI;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	public static class DataTemplateHelper
	{
		public static DataTemplate ResolveTemplate(
			DataTemplate dataTemplate,
			DataTemplateSelector dataTemplateSelector,
			object data,
			DependencyObject container)
		{
			var template = dataTemplate;
			if (template != null)
			{
				return template;
			}

			if (dataTemplateSelector != null)
			{
				var result = dataTemplateSelector.SelectTemplate(data);

				if (result == null
					&& container != null
					&& !FeatureConfiguration.DataTemplateSelector.UseLegacyTemplateSelectorOverload)
				{
					result = dataTemplateSelector.SelectTemplate(data, container);
				}

				return result;
			}

			return null;
		}
	}
}
