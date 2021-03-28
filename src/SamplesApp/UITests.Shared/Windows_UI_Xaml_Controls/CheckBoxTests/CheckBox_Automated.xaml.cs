using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.CheckBoxTests
{
	[Sample("Button")]
	public sealed partial class CheckBox_Automated : UserControl
	{
		public CheckBox_Automated()
		{
			this.InitializeComponent();
		}

		private void OnChecked(object sender, object args)
		{
			if (sender is CheckBox cb)
			{
				result.Text = $"Checked {cb.Name} {cb.IsChecked}";
			}
		}

		private void OnUnchecked(object sender, object args)
		{
			if (sender is CheckBox cb)
			{
				result.Text = $"Unchecked {cb.Name} {cb.IsChecked}";
			}
		}

		private void OnIndeterminate(object sender, object args)
		{
			if (sender is CheckBox cb)
			{
				result.Text = $"Indeterminate {cb.Name} {cb.IsChecked}";
			}
		}
	}
}
