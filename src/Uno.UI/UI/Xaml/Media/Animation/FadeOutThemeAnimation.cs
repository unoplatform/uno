using System;
using System.Threading;
using Windows.UI.Xaml.Markup;
using Uno.UI;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class FadeOutThemeAnimation : DoubleAnimation
	{
		public FadeOutThemeAnimation()
		{
			this.SetValue(
				DurationProperty,
				new Duration(FeatureConfiguration.ThemeAnimation.DefaultThemeAnimationDuration),
				DependencyPropertyValuePrecedences.DefaultValue);

			this.SetValue(
				Storyboard.TargetPropertyProperty,
				"Opacity",
				DependencyPropertyValuePrecedences.DefaultValue);

			this.SetValue(
				ToProperty,
				(double?)0d,
				DependencyPropertyValuePrecedences.DefaultValue);
		}

		public static DependencyProperty TargetNameProperty { get; } = DependencyProperty.Register(
			"TargetName", typeof(string), typeof(FadeOutThemeAnimation), new FrameworkPropertyMetadata(string.Empty));

		public string TargetName
		{
			get => (string)GetValue(TargetNameProperty);
			set => SetValue(TargetNameProperty, value);
		}

		private protected override void InitTarget()
		{
			var target = NameScope.GetNameScope(this)?.FindName(TargetName);
			if (target is DependencyObject depObj)
			{
				Storyboard.SetTarget(this, depObj);
			}
		}
	}
}
