using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_DataTemplateSelector")]
	public sealed partial class ListView_DataTemplateSelector : UserControl
	{
		public ListView_DataTemplateSelector()
		{
			this.InitializeComponent();

			DataContext = new[]
			{
				"Shape1",
				"Shape2",
				"Shape3",
				"Shape4",
				"Shape5",
			};
		}
	}
}
