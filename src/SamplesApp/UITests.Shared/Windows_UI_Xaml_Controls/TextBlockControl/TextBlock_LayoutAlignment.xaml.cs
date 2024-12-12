using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Threading.Tasks;

namespace UITests.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock")]
	public sealed partial class TextBlock_LayoutAlignment : Page
	{
		public TextBlock_LayoutAlignment()
		{
			this.InitializeComponent();

			this.Loaded += WhenLoaded;

			void WhenLoaded(object sender, RoutedEventArgs e)
			{
				//await Task.Delay(10);
				txt6_1.Text = "Stretch/Top";
				txt6_2.Text = "Stretch/Stretch";
				txt6_3.Text = "Stretch/Center";
				txt6_4.Text = "Stretch/Bottom";
			}
		}
	}
}
