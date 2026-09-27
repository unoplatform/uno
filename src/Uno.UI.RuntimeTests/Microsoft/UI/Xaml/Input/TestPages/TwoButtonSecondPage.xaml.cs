using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

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
			SecondPageFirstButton.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
	}
}
