using SamplesApp.Windows_UI_Xaml_Controls.ToggleSwitchControl.Models;
using Uno.UI.Samples.Controls;
using Uno.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
	[SampleControlInfo("ToggleSwitch", "ToggleSwitch_TemplateReuse", typeof(ToggleSwitchViewModel))]
	public sealed partial class ToggleSwitch_TemplateReuse : Page
	{
		public ToggleSwitch_TemplateReuse()
		{
			this.InitializeComponent();

			FeatureConfiguration.Page.IsPoolingEnabled = true;
			FrameworkTemplatePool.IsPoolingEnabled = true;

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
				(theStackPanel as StackPanel).Add(separatedToggleSwitch);
			};
		}
	}
}
