using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.ThemeResources
{
	[Sample("XAML", "BasicThemeResources")]
	public sealed partial class BasicThemeResources : Page
	{
		public BasicThemeResources()
		{
			InitializeComponent();

			ApplicationTheme = Application.Current.RequestedTheme;

			DataContext = this;
		}

		private ApplicationTheme ApplicationTheme { get; }

		public static DependencyProperty LocalThemeProperty { get; } = DependencyProperty.Register(
			"LocalTheme", typeof(ElementTheme), typeof(BasicThemeResources), new PropertyMetadata(default(ElementTheme)));

		public ElementTheme LocalTheme
		{
			get => RequestedTheme;
			set
			{
				SetValue(LocalThemeProperty, value);
				RequestedTheme = value;
			}
		}

		public static DependencyProperty ParentThemeProperty { get; } = DependencyProperty.Register(
			"ParentTheme", typeof(ElementTheme), typeof(BasicThemeResources), new PropertyMetadata(default(ElementTheme)));

		public ElementTheme ParentTheme
		{
			get => ((FrameworkElement)Parent).RequestedTheme;
			set
			{
				SetValue(ParentThemeProperty, value);
				((FrameworkElement)Parent).RequestedTheme = value;
			}
		}

		private void LocalDefault(object sender, RoutedEventArgs e) => LocalTheme = ElementTheme.Default;

		private void LocalDark(object sender, RoutedEventArgs e) => LocalTheme = ElementTheme.Dark;

		private void LocalLight(object sender, RoutedEventArgs e) => LocalTheme = ElementTheme.Light;

		private void ParentDefault(object sender, RoutedEventArgs e) => LocalTheme = ElementTheme.Default;

		private void ParentDark(object sender, RoutedEventArgs e) => LocalTheme = ElementTheme.Dark;

		private void ParentLight(object sender, RoutedEventArgs e) => LocalTheme = ElementTheme.Light;
	}
}
