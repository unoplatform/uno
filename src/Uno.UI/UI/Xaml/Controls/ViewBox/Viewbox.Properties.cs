using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Uno.UI;
using Windows.UI.Xaml.Media;

#if __ANDROID__
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Viewbox
	{
		public StretchDirection StretchDirection
		{
			get => (StretchDirection)this.GetValue(StretchDirectionProperty);
			set => SetValue(StretchDirectionProperty, value);
		}

		public Stretch Stretch
		{
			get => (Stretch)this.GetValue(StretchProperty);
			set => this.SetValue(StretchProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty StretchDirectionProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(StretchDirection),
			propertyType: typeof(StretchDirection),
			ownerType: typeof(Viewbox),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: StretchDirection.Both,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);

		public static global::Windows.UI.Xaml.DependencyProperty StretchProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(Stretch),
			propertyType: typeof(Stretch),
			ownerType: typeof(Viewbox),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: Stretch.Uniform,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);
	}
}
