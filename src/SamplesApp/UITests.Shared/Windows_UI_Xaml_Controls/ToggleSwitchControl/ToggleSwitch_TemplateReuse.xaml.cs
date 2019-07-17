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

		protected override void OnLoaded()
		{
			base.OnLoaded();
			SetPoolingEnabled(true);
		}

		protected override void OnUnloaded()
		{
			base.OnLoaded();
			SetPoolingEnabled(false);
		}

		private void SetPoolingEnabled(bool enabled)
		{
			FeatureConfiguration.Page.IsPoolingEnabled = enabled;
			FrameworkTemplatePool.IsPoolingEnabled = enabled;
		}
	}
}
