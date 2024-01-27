using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ContentControlTestsControl;

[SampleControlInfo(nameof(ContentControl), nameof(ContentControl_TemplateRecycling))]
public sealed partial class ContentControl_TemplateRecycling : UserControl
{
	public ContentControl_TemplateRecycling()
	{
		this.InitializeComponent();

		FeatureConfiguration.Page.IsPoolingEnabled = FrameworkTemplatePool.IsPoolingEnabled = true;
		Unloaded += (s, e) => FeatureConfiguration.Page.IsPoolingEnabled = FrameworkTemplatePool.IsPoolingEnabled = false;
	}

	private void SetTemplate(object sender, RoutedEventArgs e)
	{
		if (sender is not Button { Tag: ContentControl target } button) throw new InvalidOperationException();
		if (button.Content is not string key) throw new InvalidOperationException();
		target.ContentTemplate = key switch
		{
			"TemplateA" => Resources.TryGetValue(key, out var templateA) ? (DataTemplate)templateA : throw new ArgumentOutOfRangeException(),
			"TemplateB" => Resources.TryGetValue(key, out var templateB) ? (DataTemplate)templateB : throw new ArgumentOutOfRangeException(),
			"Null" => null,

			_ => throw new ArgumentOutOfRangeException(),
		};
	}
}
