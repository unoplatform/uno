using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

#nullable disable

namespace MobileTemplateSelectorIssue.Editors
{
	[ContentProperty(Name = nameof(Templates))]
	public class TypedDataTemplateSelector : DataTemplateSelector
	{
		public ObservableCollection<TypedDataTemplate> Templates { get; }
		  = new ObservableCollection<TypedDataTemplate>();

		public TypedDataTemplateSelector()
		{
			var incc = (INotifyCollectionChanged)Templates;
			incc.CollectionChanged += (sender, e) =>
			{
				if (e?.NewItems.Cast<TypedDataTemplate>().Any(tdt => tdt?.DataType == null || tdt?.DataTemplate == null) == true)
					throw new InvalidOperationException("All items must have all properties set.");
			};
		}

		protected override DataTemplate SelectTemplateCore(object item,
			DependencyObject container)
		{
			if (item == null)
				return null;

			if (!Templates.Any())
				throw new InvalidOperationException("No DataTemplates found.");

			var result = Templates.FirstOrDefault(t => t.DataType.IsAssignableFrom(item.GetType()));
			if (result == null)
				System.Diagnostics.Debug.WriteLine($"Could not find a matching template for type '{item.GetType()}'.");
			else
			{
				Console.WriteLine($"Matched {result.TypeName} DataTemplate {item.GetType()}");
			}

			return result?.DataTemplate;
		}
	}

	[ContentProperty(Name = nameof(DataTemplate))]
	public class TypedDataTemplate
	{
		public string TypeName { get; set; }
		public Type DataType { get; set; }
		public DataTemplate DataTemplate { get; set; }
	}
}
