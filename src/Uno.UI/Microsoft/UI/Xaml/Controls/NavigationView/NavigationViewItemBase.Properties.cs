// MUX reference NavigationViewItemBase.properties.cpp, commit de78834

using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NavigationViewItemBase
	{
		/// <summary>
		/// Gets or sets the value that indicates
		/// whether a NavigationViewItem is selected.
		/// </summary>
		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		/// <summary>
		/// Identifies the IsSelected dependency property.
		/// </summary>
		public static DependencyProperty IsSelectedProperty { get; } =
			DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(NavigationViewItemBase), new PropertyMetadata(false, OnIsSelectedPropertyChanged));

		private static void OnIsSelectedPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (NavigationViewItemBase)sender;
			owner.OnPropertyChanged(args);
		}
	}
}
