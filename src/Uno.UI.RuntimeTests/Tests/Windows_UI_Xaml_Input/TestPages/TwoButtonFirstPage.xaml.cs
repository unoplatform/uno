using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages
{
	public sealed partial class TwoButtonFirstPage : FocusNavigationPage
	{
		public TwoButtonFirstPage()
		{
			InitializeComponent();
		}

		private void GoForward() => Frame.Navigate(typeof(TwoButtonFirstPage));

		public void FocusFirst() =>
			FirstPageFirstButton.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
	}
}
