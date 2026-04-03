using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.SplitMenuFlyoutItemTests
{
	[Sample("Flyouts")]
	public sealed partial class SplitMenuFlyoutItemPage : Page
	{
		private int _clickCount;

		public SplitMenuFlyoutItemPage()
		{
			this.InitializeComponent();

			SaveItem.Click += (s, e) => BasicStatus.Text = "Primary action: Save clicked";
			OpenWithItem.Click += (s, e) => BasicStatus.Text = "Primary action: Open With Photos clicked";
			SaveAsItem.Click += (s, e) => BasicStatus.Text = "Sub-item: Save As clicked";
			ClickTestItem.Click += (s, e) =>
			{
				_clickCount++;
				ClickStatus.Text = $"Clicked {_clickCount} time(s)";
			};
		}
	}
}
