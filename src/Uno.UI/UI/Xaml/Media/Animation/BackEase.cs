using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class BackEase : EasingFunctionBase
	{
		public double Amplitude
		{
			get { return (double)this.GetValue(AmplitudeProperty); }
			set { this.SetValue(AmplitudeProperty, value); }
		}

		public static DependencyProperty AmplitudeProperty { get; } =
			DependencyProperty.Register("Amplitude", typeof(double), typeof(BackEase), new FrameworkPropertyMetadata(1d));

		// https://github.com/dotnet/wpf/blob/c3439bc20a3c2ee28d98e7d0eebcf7edcc37b7b9/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Animation/BackEase.cs#L42-L45
		private protected override double EaseInCore(double normalizedTime)
		{
			double amp = Math.Max(0.0, Amplitude);
			return Math.Pow(normalizedTime, 3.0) - normalizedTime * amp * Math.Sin(Math.PI * normalizedTime);
		}
	}
}
