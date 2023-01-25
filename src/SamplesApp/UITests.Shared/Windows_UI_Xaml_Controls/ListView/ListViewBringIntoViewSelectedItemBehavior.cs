using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	/// <summary>
	/// A behavior that, when attached to a listView, scrolls SelectedItem into view when it is selected.
	/// </summary>
	//Imported from Umbrella
	public class ListViewBringIntoViewSelectedItemBehavior
	{
		public static bool GetIsEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsEnabledProperty);
		}

		/// <summary>
		/// Set the IsEnabled attached property to true to activate the behavior.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="value"></param>
		public static void SetIsEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(IsEnabledProperty, value);
		}

		public static DependencyProperty IsEnabledProperty { get; } =
			DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ListViewBringIntoViewSelectedItemBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

		private static void OnIsEnabledChanged(object d, DependencyPropertyChangedEventArgs e)
		{

			var listView = (ListViewBase)d;

			if ((bool)e.NewValue)
			{
				listView.SelectionChanged += OnListViewSelectionChanged;
			}
			else
			{
				listView.SelectionChanged -= OnListViewSelectionChanged;
			}
		}

		private static void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var listView = (ListViewBase)sender;
			var selectedItem = listView.SelectedItem;

			listView.ScrollIntoView(selectedItem);
		}
	}
}

