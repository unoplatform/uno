using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages
{
	public sealed partial class TwoButtonSecondPage : FocusNavigationPage
	{
		public TwoButtonSecondPage()
		{
			InitializeComponent();
		}

		private void GoBackward() => Frame.GoBack();

		public void FocusFirst() =>
			SecondPageFirstButton.Focus(Windows.UI.Xaml.FocusState.Programmatic);
	}
}
