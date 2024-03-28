using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.TreeView;

public class FSObjectTemplateSelector : DataTemplateSelector
{
	public DataTemplate DirectoryTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);

	protected override DataTemplate SelectTemplateCore(object item)
	{
		return DirectoryTemplate;
	}
}

[Sample("TreeView", IsManualTest = true)]
public sealed partial class TreeViewAlignment : Page
{
	public TreeViewAlignment()
	{
		this.InitializeComponent();
	}
}
