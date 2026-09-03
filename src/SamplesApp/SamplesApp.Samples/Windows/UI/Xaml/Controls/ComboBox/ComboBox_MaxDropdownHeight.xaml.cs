using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_MaxDropdownHeight", ViewModelType = typeof(ListViewViewModel))]
	public sealed partial class ComboBox_MaxDropdownHeight : Page
	{
		public ComboBox_MaxDropdownHeight()
		{
			this.InitializeComponent();

			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(200, 200));
		}
	}
}
