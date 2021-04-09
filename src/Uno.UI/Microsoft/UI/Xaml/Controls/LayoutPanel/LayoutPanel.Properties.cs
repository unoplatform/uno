using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	partial class LayoutPanel
	{
		public static DependencyProperty LayoutProperty { get; } = DependencyProperty.Register(
			"Layout", typeof(Layout), typeof(LayoutPanel), new PropertyMetadata(default(Layout)));

		public Layout Layout
		{
			get => (Layout)GetValue(LayoutProperty);
			set => SetValue(LayoutProperty, value);
		}
	}
}
