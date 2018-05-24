using System.Windows.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleMenuFlyoutItem : MenuFlyoutItem
	{
		public ToggleMenuFlyoutItem()
		{

		}

		#region IsChecked

		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty IsCheckedProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"IsChecked", typeof(bool),
				typeof(global::Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem),
				new FrameworkPropertyMetadata(default(bool)));

		#endregion
	}
}