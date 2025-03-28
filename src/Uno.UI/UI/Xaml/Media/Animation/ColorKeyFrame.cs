using Uno;
using Uno.Extensions;
using Windows.UI;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Media.Animation
{
	public abstract partial class ColorKeyFrame : DependencyObject
	{
		protected ColorKeyFrame()
		{
			IsAutoPropertyInheritanceEnabled = true;
			InitializeBinder();
		}

		public static DependencyProperty ValueProperty { get; } = DependencyProperty.Register(
			"Value",
			typeof(Color),
			typeof(ColorKeyFrame),
			new FrameworkPropertyMetadata(default(Color)));

		public Color Value
		{
			get => (Color)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static DependencyProperty KeyTimeProperty { get; } = DependencyProperty.Register(
			"KeyTime",
			typeof(KeyTime),
			typeof(ColorKeyFrame),
			new FrameworkPropertyMetadata(default(KeyTime)));

		public KeyTime KeyTime
		{
			get => (KeyTime)GetValue(KeyTimeProperty);
			set => SetValue(KeyTimeProperty, value);
		}

		internal virtual IEasingFunction GetEasingFunction() => null;

		public override string ToString() => "KeyTime: {0}, Value: {1}".InvariantCultureFormat(KeyTime, Value);
	}
}
