using SampleControl.Presentation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Linq;

namespace SamplesApp
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

            ComboBoxTest.ItemsSource = global::System.Linq.Enumerable.Range(1, 3).Select(x => $"Item 1.{x}");
		}
	}
}
