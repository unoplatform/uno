using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ButtonBase.Models
{
	[ContentProperty(Name = "Templates")]
	public class StringKeyTemplateSelector : DataTemplateSelector
	{
		public StringKeyTemplateSelectorCollection ContainerTemplates { get; set; } = new StringKeyTemplateSelectorCollection();
		public StringKeyTemplateSelectorCollection Templates { get; set; } = new StringKeyTemplateSelectorCollection();

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return SelectDataTemplate(item, ContainerTemplates);
		}

		protected override DataTemplate SelectTemplateCore(object item)
		{
			return SelectDataTemplate(item, Templates);
		}

		private static DataTemplate SelectDataTemplate(object item, StringKeyTemplateSelectorCollection templates)
		{
			if (item is string keyStr)
			{
				return templates.FirstOrDefault(x => x.TemplateKey == keyStr)?.DataTemplate;
			}

			if (templates != null && templates.Any())
			{
				return (templates.FirstOrDefault(x => x.IsDefaultTemplate) ?? templates.FirstOrDefault())?.DataTemplate;
			}

			return null;
		}
	}

	public class StringKeyTemplateSelectorCollection : List<StringTemplateSelectorItem>
	{
	}

	[ContentProperty(Name = "DataTemplate")]
	public class StringTemplateSelectorItem
	{
		public string TemplateKey { get; set; }
		public DataTemplate DataTemplate { get; set; }
		public bool IsDefaultTemplate { get; set; }
	}
}
