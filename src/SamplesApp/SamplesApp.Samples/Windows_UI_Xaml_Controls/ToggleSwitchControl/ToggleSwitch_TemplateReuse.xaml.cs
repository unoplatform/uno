using SamplesApp.Windows_UI_Xaml_Controls.ToggleSwitchControl.Models;
using Uno.UI.Samples.Controls;
using Uno.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
	[Sample("ToggleSwitch", Name = "ToggleSwitch_TemplateReuse", ViewModelType = typeof(ToggleSwitchViewModel))]
	public sealed partial class ToggleSwitch_TemplateReuse : Page
	{
		public ToggleSwitch_TemplateReuse()
		{
			this.InitializeComponent();

			SetPoolingEnabled(true);

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
				(theStackPanel as StackPanel).Children.Add(separatedToggleSwitch);
			};

			unload.Unloaded += (snd, evt) =>
			{
				SetPoolingEnabled(false);
			};
		}

		private void SetPoolingEnabled(bool enabled)
		{
#if __ANDROID || __APPLE_UIKIT__
			FeatureConfiguration.Page.IsPoolingEnabled = enabled;
			FrameworkTemplatePool.IsPoolingEnabled = enabled;
#endif
		}
	}
}
