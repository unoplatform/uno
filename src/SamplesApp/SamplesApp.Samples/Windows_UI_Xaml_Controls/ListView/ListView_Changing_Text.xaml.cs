using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_Changing_Text", ViewModelType = typeof(ListViewViewModel), Description = "On Android, the text doesn't always change when toggling the CheckBox.")]
	public sealed partial class ListView_Changing_Text : UserControl
	{
		public ListView_Changing_Text()
		{
			this.InitializeComponent();
		}
	}
}
