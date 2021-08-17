using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RadioMenuFlyoutItem
	{
		public static bool GetAreCheckStatesEnabled(DependencyObject obj) => (bool)obj.GetValue(AreCheckStatesEnabledProperty);
		public static void SetAreCheckStatesEnabled(DependencyObject obj, bool value) => obj.SetValue(AreCheckStatesEnabledProperty, value);

		public static readonly DependencyProperty AreCheckStatesEnabledProperty =
			DependencyProperty.RegisterAttached("AreCheckStatesEnabled", typeof(bool), typeof(RadioMenuFlyoutItem), new FrameworkPropertyMetadata(false, OnAreCheckStatesEnabledPropertyChanged));

		public string GroupName
		{
			get { return (string)GetValue(GroupNameProperty); }
			set { SetValue(GroupNameProperty, value); }
		}

		public static readonly DependencyProperty GroupNameProperty =
			DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(RadioMenuFlyoutItem), new FrameworkPropertyMetadata(string.Empty, (s, e) => (s as RadioMenuFlyoutItem)?.OnPropertyChanged(e)));

		public new bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public static new readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(RadioMenuFlyoutItem), new FrameworkPropertyMetadata(false, (s, e) => (s as RadioMenuFlyoutItem)?.OnPropertyChanged(e)));
	}
}
