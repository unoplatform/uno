using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ContentPresenterPages
{
	public sealed partial class ContentPresenter_Inside_ContentControlTemplate : UserControl
	{
		public ContentPresenter_Inside_ContentControlTemplate()
		{
			this.InitializeComponent();
		}
	}

	public class ItemViewModel
	{
		public string Text { get; set; } = "";
		public bool IsRed { get; set; } = false;
	}

	public class ItemTemplateSelector : DataTemplateSelector
	{
		public DataTemplate RedTemplate { get; set; } = default!;
		public DataTemplate GreenTemplate { get; set; } = default!;

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return (item as ItemViewModel)?.IsRed ?? true ? RedTemplate : GreenTemplate;
		}
	}
}
