using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
	[Sample("ToggleSwitch", Name = "ToggleSwitchUnloadReload")]
	public sealed partial class ToggleSwitchUnloadReload : Page
	{
		public ToggleSwitchUnloadReload()
		{
			this.InitializeComponent();

			var c = root.Child;

			unload.Tapped += (snd, evt) =>
			{
				c = root.Child;
				root.Child = null;
				unload.IsEnabled = false;
				reload.IsEnabled = true;
			};

			reload.Tapped += (snd, evt) =>
			{
				root.Child = c;
				unload.IsEnabled = true;
				reload.IsEnabled = false;
			};
		}
	}
}
