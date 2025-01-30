using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

#nullable disable

namespace FrameworkPoolEditorRecycling.Editors;

[ContentProperty(Name = nameof(Templates))]
public class EditorTemplateSelector : DataTemplateSelector
{
	public ObservableCollection<TypedDataTemplate> Templates { get; }
	  = new ObservableCollection<TypedDataTemplate>();

	public EditorTemplateSelector()
	{
	}

	protected override DataTemplate SelectTemplateCore(object item,
		DependencyObject container)
	{
		if (item is not EditorViewModel editor)
			return null;

		if (!Templates.Any())
			throw new InvalidOperationException("No DataTemplates found.");

		var result = Templates.FirstOrDefault(t => t.TypeName == editor.EditorViewType.Name);
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

	public DataTemplate DataTemplate { get; set; }
}
