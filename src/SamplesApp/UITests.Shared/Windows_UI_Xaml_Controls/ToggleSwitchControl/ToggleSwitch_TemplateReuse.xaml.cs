using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
	[SampleControlInfo("ToggleSwitch", "ToggleSwitch_TemplateReuse")]
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
#if !WINDOWS_UWP
				PropagateOnTemplateReusedTest(separatedToggleSwitch);
#endif
			};
		}

		// Test is a copy of method PropagateOnTemplateReused which is inaccessible due to protection level and isn't called due to no caching in Samples
		// Therefore this method
#if !WINDOWS_UWP
		internal static void PropagateOnTemplateReusedTest(ToggleSwitch instance)
		{
			if (instance is ToggleSwitch templateAwareElement && (instance as IFrameworkElement).DataContext == null)
			{
				templateAwareElement.OnTemplateRecycled();
			}
		}
#endif
	}
}
