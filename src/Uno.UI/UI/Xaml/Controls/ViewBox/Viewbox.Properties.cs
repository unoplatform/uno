using Windows.Foundation;
using Microsoft.UI.Xaml.Markup;
using Uno.UI;
using Microsoft.UI.Xaml.Media;

#if __ANDROID__
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Microsoft.UI.Xaml.Controls
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

		public static global::Microsoft.UI.Xaml.DependencyProperty StretchDirectionProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			name: nameof(StretchDirection),
			propertyType: typeof(StretchDirection),
			ownerType: typeof(Viewbox),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: StretchDirection.Both,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);

		public static global::Microsoft.UI.Xaml.DependencyProperty StretchProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
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
