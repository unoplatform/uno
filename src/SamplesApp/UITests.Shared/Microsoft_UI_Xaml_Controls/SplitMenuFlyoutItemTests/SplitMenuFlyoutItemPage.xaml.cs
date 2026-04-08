using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.SplitMenuFlyoutItemTests
{
#if HAS_UNO
	[Sample("Flyouts")]
#endif
	public sealed partial class SplitMenuFlyoutItemPage : Page
	{
#if HAS_UNO
		private int _clickCount;
#endif

		public SplitMenuFlyoutItemPage()
		{
			this.InitializeComponent();

#if HAS_UNO
			SaveItem.Click += (s, e) => BasicStatus.Text = "Primary action: Save clicked";
			OpenWithItem.Click += (s, e) => BasicStatus.Text = "Primary action: Open With Photos clicked";
			SaveAsItem.Click += (s, e) => BasicStatus.Text = "Sub-item: Save As clicked";
			ClickTestItem.Click += (s, e) =>
			{
				_clickCount++;
				ClickStatus.Text = $"Clicked {_clickCount} time(s)";
			};
#endif
		}
	}
}
