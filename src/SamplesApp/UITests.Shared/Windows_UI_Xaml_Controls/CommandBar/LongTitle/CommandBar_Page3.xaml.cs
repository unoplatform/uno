using System;
using System.Threading.Tasks;
using Windows.UI;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.CommandBar.LongTitle
{
	public sealed partial class CommandBar_Page3 : Page
	{
		public CommandBar_Page3()
		{
			this.InitializeComponent();
		}

		public void OnButtonClicked(object sender, RoutedEventArgs e)
			=> Frame.GoBack();
	}
}
