using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Popup
{
	[SampleControlInfo("Popup", "Popup_LightDismiss", description: "Once opened, each popup should be dismissed one after the other, starting by the fourth one.")]
	public sealed partial class Popup_LightDismiss : Page
	{
		public Popup_LightDismiss()
		{
			this.InitializeComponent();

			btn4.Tapped += (snd, evt) =>
			{
				popup1.IsOpen = true;
				popup2.IsOpen = true;
				popup3.IsOpen = true;
				popup4.IsOpen = true;
			};

			btn4R.Tapped += (snd, evt) =>
			{
				popup4.IsOpen = true;
				popup3.IsOpen = true;
				popup2.IsOpen = true;
				popup1.IsOpen = true;
			};

			btn5.Tapped += (snd, evt) =>
			{
				popup1.IsOpen = true;
				popup2.IsOpen = true;
				popup3.IsOpen = true;
				popup4.IsOpen = true;
				popup5.IsOpen = true;
			};

			close5.Tapped += (snd, evt) => popup5.IsOpen = false;
		}
	}
}
