using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Content))]
	public partial class Flyout : FlyoutBase
	{
		public Style FlyoutPresenterStyle
		{
			get { return (Style)this.GetValue(FlyoutPresenterStyleProperty); }
			set { this.SetValue(FlyoutPresenterStyleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FlyoutPresenterStyle.  This enables animation, styling, binding, etc...
		public static DependencyProperty FlyoutPresenterStyleProperty { get; } =
			DependencyProperty.Register(
				"FlyoutPresenterStyle",
				typeof(Style),
				typeof(Flyout),
				new FrameworkPropertyMetadata(default(Style), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure));

		public UIElement Content
		{
			get { return (UIElement)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		public static DependencyProperty ContentProperty { get; } =
			DependencyProperty.Register(
				"Content",
				typeof(UIElement),
				typeof(Flyout),
				new FrameworkPropertyMetadata(default(UIElement), FrameworkPropertyMetadataOptions.AffectsMeasure));

		public Flyout()
		{
		}
	}
}
