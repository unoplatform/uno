using Uno;
using Uno.Extensions;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Media.Animation
{
	public abstract partial class ColorKeyFrame : DependencyObject
	{
		public ColorKeyFrame()
		{
			IsAutoPropertyInheritanceEnabled = true;
			InitializeBinder();
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value",
			typeof(Color),
			typeof(ColorKeyFrame),
			new PropertyMetadata(default(Color)));

		public Color Value
		{
			get => (Color)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static readonly DependencyProperty KeyTimeProperty = DependencyProperty.Register(
			"KeyTime",
			typeof(KeyTime),
			typeof(ColorKeyFrame),
			new PropertyMetadata(default(KeyTime)));

		public KeyTime KeyTime
		{
			get => (KeyTime)GetValue(KeyTimeProperty);
			set => SetValue(KeyTimeProperty, value);
		}

		internal virtual IEasingFunction GetEasingFunction() => null;

		public override string ToString() => "KeyTime: {0}, Value: {1}".InvariantCultureFormat(KeyTime, Value);
	}
}
