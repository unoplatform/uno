#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewItem : NavigationViewItemBase
	{
		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public double CompactPaneLength
			=> (double)GetValue(CompactPaneLengthProperty);

		public static global::Windows.UI.Xaml.DependencyProperty CompactPaneLengthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"CompactPaneLength", typeof(double),
			typeof(NavigationViewItem),
			new FrameworkPropertyMetadata(defaultValue: 48.0)
		);

		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: "Icon",
			propertyType: typeof(IconElement),
			ownerType: typeof(NavigationViewItem),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: null)
		);

		public NavigationViewItem()
		{
			DefaultStyleKey = typeof(NavigationViewItem);
		}

		internal Rectangle SelectionIndicator { get; private set; }

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			InternalPointerPressed?.Invoke();
		}

		internal event Action InternalPointerPressed;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			SelectionIndicator = GetTemplateChild("SelectionIndicator") as Rectangle;
		}
	}
}
