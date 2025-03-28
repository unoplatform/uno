using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages
{
	public sealed partial class ListViewItemPage : FocusNavigationPage
	{
		public ListViewItemPage()
		{
			InitializeComponent();
			TestListView.ItemsSource = new int[] { 1, 2, 3 };
		}

		public ListView List => TestListView;
	}
}
