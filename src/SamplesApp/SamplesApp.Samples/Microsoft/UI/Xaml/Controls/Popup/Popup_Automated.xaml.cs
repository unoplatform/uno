using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Popup
{
	[Sample("Popup", Name = nameof(Popup_Automated))]
	public sealed partial class Popup_Automated : UserControl
	{
		public Popup_Automated()
		{
			this.InitializeComponent();
		}

		private void OpenDismissablePopup(object sender, RoutedEventArgs args)
		{
			this.DismissablePopup.IsOpen = true;
		}
		private void CloseDismissablePopup(object sender, RoutedEventArgs args)
		{
			this.DismissablePopup.IsOpen = false;
		}

		private void OpenNonDismissablePopup(object sender, RoutedEventArgs args)
		{
			this.NonDismissablePopup.IsOpen = true;
		}
		private void CloseNonDismissablePopup(object sender, RoutedEventArgs args)
		{
			this.NonDismissablePopup.IsOpen = false;
		}

		private void OpenNoFixedHeightPopup(object sender, RoutedEventArgs args)
		{
			this.NoFixedHeightPopup.IsOpen = true;
		}
		private void CloseNoFixedHeightPopup(object sender, RoutedEventArgs args)
		{
			this.NoFixedHeightPopup.IsOpen = false;
		}
	}
}
