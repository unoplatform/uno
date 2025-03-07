using System;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class ElasticEase : EasingFunctionBase
	{
		public double Springiness
		{
			get => (double)this.GetValue(SpringinessProperty);
			set => this.SetValue(SpringinessProperty, value);
		}

		public int Oscillations
		{
			get => (int)this.GetValue(OscillationsProperty);
			set => this.SetValue(OscillationsProperty, value);
		}

		public static DependencyProperty OscillationsProperty { get; } =
			DependencyProperty.Register(
				nameof(Oscillations),
				typeof(int),
				typeof(ElasticEase),
				new FrameworkPropertyMetadata(3));

		public static DependencyProperty SpringinessProperty { get; } =
			DependencyProperty.Register(
				nameof(Springiness),
				typeof(double),
				typeof(ElasticEase),
				new FrameworkPropertyMetadata(3.0));

		// https://github.com/dotnet/wpf/blob/ebe5937b557c3fa9cb29fb3a417c71652573c7e4/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Animation/ElasticEase.cs
		private protected override double EaseInCore(double normalizedTime)
		{
			double oscillations = Math.Max(0.0, (double)Oscillations);
			double springiness = Math.Max(0.0, Springiness);
			double expo;
			if (springiness.IsZero())
			{
				expo = normalizedTime;
			}
			else
			{
				expo = (Math.Exp(springiness * normalizedTime) - 1.0) / (Math.Exp(springiness) - 1.0);
			}

			return expo * (Math.Sin((Math.PI * 2.0 * oscillations + Math.PI * 0.5) * normalizedTime));
		}
	}
}
